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

            var compiledBlock = codeBlock.CreateCodeBlock(unit, newFunction, $"entry_{ident.Dump()}");

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
                    unit.Messages.Log(CompilerErrorKind.Error_MissingOutputAssignment, $"The function '{ident.Dump()}' does not assign a result to the output '{o.Identifier}'.");
                }
            }
        }
        public bool IsFunctionType => true;

        public string Dump()
        {
            return $"({inputList.Dump()}) ({outputList.Dump()})";
        }
    }
}

