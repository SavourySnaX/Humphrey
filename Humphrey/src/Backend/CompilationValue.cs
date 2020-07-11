using LLVMSharp.Interop;

namespace Humphrey.Backend
{
    public class CompilationValue : ICompilationValue
    {
        LLVMValueRef valueRef;
        CompilationType typeRef;

        public CompilationValue(LLVMValueRef val, CompilationType type)
        {
            valueRef = val;
            typeRef = type;
        }

        public LLVMValueRef BackendValue => valueRef;
        public LLVMTypeRef BackendType => typeRef.BackendType;

        public CompilationType Type => typeRef;
    }
}
