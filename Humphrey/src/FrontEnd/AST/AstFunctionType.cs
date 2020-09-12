using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public class AstFunctionType : IType
    {
        AstParamList inputList;
        AstParamList outputList;
        public AstFunctionType(AstParamList inputs, AstParamList outputs)
        {
            inputList = inputs;
            outputList = outputs;
        }
    
        public (CompilationType compilationType, IType originalType) CreateOrFetchType(CompilationUnit unit)
        {
            var inputs = inputList.FetchParamList(unit);
            var outputs = outputList.FetchParamList(unit);

            return (unit.CreateFunctionType(this, inputs, outputs), this);
        }
    
        public void BuildFunction(CompilationUnit unit, CompilationFunctionType functionType, AstIdentifier ident, AstCodeBlock codeBlock)
        {
            var newFunction = unit.CreateFunction(functionType, ident);

            unit.PushScope("", unit.GetScope(newFunction));

            var localsBlock = new CompilationBlock(newFunction.BackendValue.AppendBasicBlock($"inputs_{ident.Dump()}"));
            var localsBuilder = unit.CreateBuilder(newFunction, localsBlock);

            newFunction.ExitBlock = new CompilationBlock(newFunction.BackendValue.AppendBasicBlock($"exit_{ident.Dump()}"));
            var exitBlockBuilder = unit.CreateBuilder(newFunction, newFunction.ExitBlock);

            // create an entry block and a set of locals
            for (uint a = 0; a < functionType.InputCount; a++)
            {
                var paramIdent = functionType.Parameters[a].Identifier;

                // Local copy
                var type = functionType.Parameters[a].Type;
                var local = unit.CreateLocalVariable(unit, localsBuilder, type, paramIdent, null, new SourceLocation(functionType.Parameters[a].Token));
                var cv = new CompilationValue(newFunction.BackendValue.Params[a], type);
                localsBuilder.Store(cv, local.Storage);

                // Debug information
                var paramLocation = new SourceLocation(this.inputList.FetchParamLocation(a));
                var debugType = functionType.Parameters[a].DebugType;
                var paramVar = unit.CreateParameterVariable(paramIdent, a, paramLocation, debugType);

                unit.InsertDeclareAtEnd(local.Storage, paramVar, paramLocation, localsBlock);
            }
            // allocate the output locals
            for (uint a = functionType.OutParamOffset; a < functionType.Parameters.Length; a++)
            {
                // Temporary local storage
                var outputType = unit.CreatePointerType(functionType.Parameters[a].Type, new SourceLocation(functionType.Parameters[a].Token));
                var output = new CompilationValue(newFunction.BackendValue.Params[a], outputType);
                var cv = localsBuilder.Load(output);
                var type = functionType.Parameters[a].Type;
                var paramIdent = functionType.Parameters[a].Identifier;
                var local = unit.CreateLocalVariable(unit, localsBuilder, type, paramIdent, cv, new SourceLocation(functionType.Parameters[a].Token));
                local.Storage = new CompilationValueOutputParameter(local.Storage.BackendValue, local.Storage.Type, paramIdent);

                // Copy temporary storage to output
                var returnValue = exitBlockBuilder.Load(local);
                exitBlockBuilder.Store(returnValue, output);
                
                // Debug information
                var paramLocation = new SourceLocation(this.outputList.FetchParamLocation(a - functionType.OutParamOffset));
                var debugType = functionType.Parameters[a].DebugType;
                var paramVar = unit.CreateParameterVariable(paramIdent, a, paramLocation, debugType);

                unit.InsertDeclareAtEnd(local.Storage, paramVar, paramLocation, localsBlock);
            }

            // single point of return for all functions
            exitBlockBuilder.ReturnVoid();

            var compiledBlock = codeBlock.CreateCodeBlock(unit, newFunction, $"entry_{ident.Dump()}");
            
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

            unit.PopScope();
        }
        public bool IsFunctionType => true;

        public string Dump()
        {
            return $"({inputList.Dump()}) ({outputList.Dump()})";
        }
        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}

