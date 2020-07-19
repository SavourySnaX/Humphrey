using LLVMSharp.Interop;

namespace Humphrey.Backend
{
    public class CompilationValue : ICompilationValue
    {
        LLVMValueRef valueRef;
        CompilationValue storage;
        CompilationType typeRef;

        public CompilationValue(LLVMValueRef val, CompilationType type)
        {
            valueRef = val;
            typeRef = type;
            storage = null;
        }

        public LLVMValueRef BackendValue => valueRef;
        public LLVMTypeRef BackendType => typeRef.BackendType;

        public CompilationValue Storage 
        {
            get { return storage; }
            set { storage = value; }
        }
        public CompilationType Type => typeRef;
    }
}
