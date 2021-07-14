using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public class AstFunctionType : IType, ISymbolScope /*Used for input and output symbols currently*/
    {
        AstParamList inputList;
        AstParamList outputList;
        private bool semanticDone;

        AstCodeBlock genericInitialiser;
        string genericBaseName;

        public AstFunctionType(AstParamList inputs, AstParamList outputs)
        {
            inputList = inputs;
            outputList = outputs;
            semanticDone = false;
            genericInitialiser = null;
            genericBaseName = null;
        }
    
        public void SetGenericInitialiser(AstCodeBlock codeBlock, string baseName)
        {
            if (IsGeneric)
            {
                genericInitialiser = codeBlock;
                if (genericBaseName!=null)
                    genericBaseName = $"{genericBaseName}_{baseName}";
                else
                    genericBaseName = $"___{baseName}"; // TODO - Ban use of leading underscores to reserve them for compiler use
            }
            else
                throw new System.InvalidOperationException($"Attempting to set initialiser on non generic function");
        }

        public (CompilationFunctionType compilationType, IType originalType) CreateOrFetchType(CompilationUnit unit, IType[] inputTypes)
        {
            var inputs = inputList.FetchParamList(unit, inputTypes);
            var outputs = outputList.FetchParamList(unit);

            if (metaData!=null)
            {
                if (metaData.Contains("C_CALLING_CONVENTION"))
                {
                    // We should treat this function as being an external function and thus needs resolving at link time?
                    return (unit.CreateExternalCFunctionType(this, inputs, outputs), this);
                }
            }

            return (unit.CreateFunctionType(this, inputs, outputs), this);
        }
        public (CompilationType compilationType, IType originalType) CreateOrFetchType(CompilationUnit unit)
        {
            var inputs = inputList.FetchParamList(unit);
            var outputs = outputList.FetchParamList(unit);

            if (metaData!=null)
            {
                if (metaData.Contains("C_CALLING_CONVENTION"))
                {
                    // We should treat this function as being an external function and thus needs resolving at link time?
                    return (unit.CreateExternalCFunctionType(this, inputs, outputs), this);
                }
            }

            return (unit.CreateFunctionType(this, inputs, outputs), this);
        }
    
        public void BuildFunction(CompilationUnit unit, CompilationFunctionType functionType, AstIdentifier ident)
        {
            // TODO generic output types???
            if (!IsGeneric)
            {
                throw new System.InvalidOperationException($"Cannot use this version for non generic functionTypes");
            }
            if (IsGeneric && genericInitialiser==null)
            {
                throw new System.InvalidOperationException($"genericInitialiser should have been filled during semantic pass");
            }
            var newFunction = unit.BeginCreateGenericFunction(functionType, ident);
            BuildFunction(unit, functionType, ident, genericInitialiser, newFunction.function);
            unit.EndCreateGenericFunction(newFunction.symbolTable);
        }

        public void BuildFunction(CompilationUnit unit, CompilationFunctionType functionType, AstIdentifier ident, AstCodeBlock codeBlock)
        {
            var newFunction = unit.CreateFunction(functionType, ident);
            BuildFunction(unit, functionType, ident, codeBlock, newFunction);
        }

        private void BuildFunction(CompilationUnit unit, CompilationFunctionType functionType, AstIdentifier ident, AstCodeBlock codeBlock, CompilationFunction newFunction)
        {
            var oldScope = unit.PushScope(symbolTable, unit.GetScope(newFunction));

            var localsBlock = new CompilationBlock(unit.AppendNewBasicBlockToFunction(newFunction,$"inputs_{ident.Dump()}"));
            var localsBuilder = unit.CreateBuilder(newFunction, localsBlock);
            localsBuilder.SetDebugLocation(new SourceLocation(codeBlock.BlockStart));

            newFunction.ExitBlock = new CompilationBlock(unit.AppendNewBasicBlockToFunction(newFunction,$"exit_{ident.Dump()}"));
            var exitBlockBuilder = unit.CreateBuilder(newFunction, newFunction.ExitBlock);
            exitBlockBuilder.SetDebugLocation(new SourceLocation(codeBlock.BlockEnd));

            // create an entry block and a set of locals
            for (uint a = 0; a < functionType.InputCount; a++)
            {
                var paramIdent = functionType.Parameters[a].Identifier;

                // Local copy
                var type = functionType.Parameters[a].Type;
                var local = unit.CreateLocalVariable(unit, localsBuilder, localsBuilder, type, paramIdent, null, functionType.Parameters[a].Token).cv;
                var cv = new CompilationValue(newFunction.BackendValue.Params[a], type, functionType.Parameters[a].Token);
                localsBuilder.Store(cv, local.Storage);

                // Debug information
                var paramLocation = new SourceLocation(this.inputList.FetchParamLocation(a));
                var debugType = functionType.Parameters[a].DebugType;
                // Args count from 1 (0 is return type)
                var paramVar = unit.CreateParameterVariable(paramIdent.Dump(), a + 1, paramLocation, debugType);

                unit.InsertDeclareAtEnd(local.Storage, paramVar, paramLocation, localsBlock);
            }
            // allocate the output locals
            for (uint a = functionType.OutParamOffset; a < functionType.Parameters.Length; a++)
            {
                // Temporary local storage
                var outputType = unit.CreatePointerType(functionType.Parameters[a].Type, new SourceLocation(functionType.Parameters[a].Token));
                var output = new CompilationValue(newFunction.BackendValue.Params[a], outputType, functionType.Parameters[a].Token);
                var type = functionType.Parameters[a].Type;
                var paramIdent = functionType.Parameters[a].Identifier;
                var local = unit.CreateLocalVariable(unit, localsBuilder, localsBuilder, type, paramIdent, null, functionType.Parameters[a].Token).cv;
                local.Storage = new CompilationValueOutputParameter(local.Storage.BackendValue, local.Storage.Type, paramIdent.Dump(), functionType.Parameters[a].Token);

                // Copy temporary storage to output
                var returnValue = exitBlockBuilder.Load(local);
                exitBlockBuilder.Store(returnValue, output);
                
                // Debug information
                var paramLocation = new SourceLocation(this.outputList.FetchParamLocation(a - functionType.OutParamOffset));
                var debugType = functionType.Parameters[a].DebugType;
                // Args count from 1 (0 is return type)
                var paramVar = unit.CreateParameterVariable(paramIdent.Dump(), a + 1, paramLocation, debugType);

                unit.InsertDeclareAtEnd(local.Storage, paramVar, paramLocation, localsBlock);
            }

            // single point of return for all functions
            exitBlockBuilder.ReturnVoid();

            var compiledBlock = codeBlock.CreateCodeBlock(unit, newFunction, localsBuilder, $"entry_{ident.Dump()}");
            
            // LocalsBuilder needs to jump to compiledBlock
            localsBuilder.Branch(compiledBlock.entry);

            if (compiledBlock.exit.BackendValue.Terminator == null)
            {
                var builder = unit.CreateBuilder(newFunction, compiledBlock.exit);
                builder.Branch(newFunction.ExitBlock);
            }

            // Now we need to know if all outputs were stored.... this check is crude, will not deal with conditions/loops etc
            if (!newFunction.AreOutputsAllUsed())
            {
                foreach (var o in newFunction.FetchMissingOutputs())
                {
                    unit.Messages.Log(CompilerErrorKind.Error_MissingOutputAssignment, $"The function '{ident.Dump()}' does not assign a result to the output '{o.Identifier}'.", o.Token.Location, o.Token.Remainder);
                }
            }

            unit.PopScope(oldScope);
        }

        public IType ResolveOutputType(SemanticPass pass)
        {
            var outputs = outputList.Params;
            if (outputs.Length==0)
            {
                return null;
            }
            if (metaData != null)
            {
                if (metaData.Contains("C_CALLING_CONVENTION"))
                {
                    if (outputs.Length == 1)
                    {
                        return outputs[0].Type;
                    }
                    else
                    {
                        throw new System.NotImplementedException($"TODO - error illegal for c functions to return multiple params");
                    }
                }
            }
            var astStructMembers = new AstStructElement[outputs.Length];
            for (int a=0;a<outputs.Length;a++)
            {
                astStructMembers[a] = new AstStructElement(new[] { new AstIdentifier(outputs[a].Identifier.Name) }, outputs[a].Type, null);
            }
            var str = new AstStructureType(astStructMembers, true);
            return str;
        }

        public void Semantic(SemanticPass pass)
        {
            // do nothing
        }
        
        public void Semantic(SemanticPass pass, AstCodeBlock codeBlock)
        {
            symbolTable = pass.PushScope();

            foreach (var input in inputList.Params)
            {
                if (!pass.AddValue(input.Identifier, SemanticPass.IdentifierKind.FunctionParam, input.Type, !semanticDone))
                {
                    pass.Messages.Log(CompilerErrorKind.Error_DuplicateSymbol, $"A symbol called {input.Identifier.Name} already exists", input.Identifier.Token.Location, input.Identifier.Token.Remainder);
                }
                input.Type.Semantic(pass);
            }
            foreach (var output in outputList.Params)
            {
                if (!pass.AddValue(output.Identifier, SemanticPass.IdentifierKind.FunctionParam, output.Type, !semanticDone))
                {
                    pass.Messages.Log(CompilerErrorKind.Error_DuplicateSymbol, $"A symbol called {output.Identifier.Name} already exists", output.Identifier.Token.Location, output.Identifier.Token.Remainder);
                }
                output.Type.Semantic(pass);
            }
            semanticDone = true;
            codeBlock?.Semantic(pass);

            pass.PopScope();
        }

        public bool IsFunctionType => true;

        public bool IsGeneric => inputList.HasGenericParameters() || outputList.HasGenericParameters();
        public string Dump()
        {
            return $"({inputList.Dump()}) ({outputList.Dump()})";
        }

        public IType ResolveBaseType(SemanticPass pass)
        {
            return this;
        }

        public string GenericBaseName => genericBaseName;

        public AstParamList Inputs => inputList;
        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

        private AstMetaData metaData;
        public AstMetaData MetaData { get => metaData; set => metaData = value; }

        public SemanticPass.IdentifierKind GetBaseType => SemanticPass.IdentifierKind.Function;

        private CommonSymbolTable symbolTable;
        public CommonSymbolTable SymbolTable => symbolTable;
    }
}

