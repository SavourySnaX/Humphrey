using LLVMSharp.Interop;

namespace Humphrey.Backend
{
    public class CompilationBuilder
    {
        LLVMBuilderRef builderRef;
        CompilationFunction function;

        public CompilationBuilder(LLVMBuilderRef builder, CompilationFunction func)
        {
            builderRef = builder;
            function = func;
        }

        public LLVMBuilderRef BackendValue => builderRef;
        public CompilationFunction Function => function;
    }
}
