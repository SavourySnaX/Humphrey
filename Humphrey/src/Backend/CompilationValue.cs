using LLVMSharp.Interop;

namespace Humphrey.Backend
{
    public class CompilationValue
    {
        LLVMValueRef valueRef;

        public CompilationValue(LLVMValueRef val)
        {
            valueRef = val;
        }

        public LLVMValueRef BackendValue => valueRef;
    }
}
