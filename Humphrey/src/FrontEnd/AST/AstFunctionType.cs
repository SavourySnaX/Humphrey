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
    
        public CompilationType CreateOrFetchType(CompilationUnit unit)
        {
            var inputs = inputList.FetchParamList(unit);
            var outputs = outputList.FetchParamList(unit);

            return unit.CreateFunctionType(inputs, outputs);

            throw new System.NotImplementedException($"Unimplemented Type create/fetch");
        }
    
        public static void BuildFunction(CompilationUnit unit, CompilationFunctionType functionType, AstIdentifier ident, AstCodeBlock codeBlock)
        {
            var newFunction = unit.CreateFunction(functionType, ident.Dump());

            unit.PushScope("");

            var localsBlock = new CompilationBlock(newFunction.BackendValue.AppendBasicBlock($"inputs_{ident.Dump()}"));
            var localsBuilder = unit.CreateBuilder(newFunction, localsBlock);

            // create an entry block and a set of locals
            for (uint a = 0; a < functionType.InputCount; a++)
            {
                var paramIdent = functionType.Parameters[a].Identifier;
                var type = functionType.Parameters[a].Type;
                var local = unit.CreateLocalVariable(unit, localsBuilder, type, paramIdent, null);
                var cv = new CompilationValue(newFunction.BackendValue.Params[a], type);
                localsBuilder.Store(cv, local.Storage);
            }
            // allocate the output locals - these can be mapped directly
            for (uint a = functionType.OutParamOffset; a < functionType.Parameters.Length; a++)
            {
                var outputType = new CompilationPointerType(Extensions.Helpers.CreatePointerType(functionType.Parameters[a].Type.BackendType), functionType.Parameters[a].Type);
                var output = new CompilationValueOutputParameter(newFunction.BackendValue.Params[a], outputType, functionType.Parameters[a].Identifier);
                output.Storage = output;
                unit.AddValue(functionType.Parameters[a].Identifier, output);
            }


            var compiledBlock = codeBlock.CreateCodeBlock(unit, newFunction, $"entry_{ident.Dump()}");
            
            // LocalsBuilder needs to jump to compiledBlock
            localsBuilder.BackendValue.BuildBr(compiledBlock.entry.BackendValue);

            if (compiledBlock.exit.BackendValue.Terminator == null)
            {
                var builder = unit.CreateBuilder(newFunction, compiledBlock.exit);
                builder.BackendValue.BuildRetVoid();
            }

            // Now we need to know if all outputs were stored....
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

