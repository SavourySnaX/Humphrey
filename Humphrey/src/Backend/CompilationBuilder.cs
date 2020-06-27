using LLVMSharp.Interop;

namespace Humphrey.Backend
{
    public class CompilationBuilder
    {
        LLVMBuilderRef builderRef;

        public CompilationBuilder(LLVMBuilderRef builder)
        {
            builderRef = builder;
        }

        public LLVMBuilderRef BackendValue => builderRef;
    }
}
