using LLVMSharp.Interop;

namespace Humphrey.Backend
{
    public class CompilationBlock
    {
        LLVMBasicBlockRef basicBlockRef;

        public CompilationBlock(LLVMBasicBlockRef bb)
        {
            basicBlockRef = bb;
        }

        public LLVMBasicBlockRef BackendValue => basicBlockRef;
    }
}