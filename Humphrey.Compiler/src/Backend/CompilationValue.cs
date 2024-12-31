using Humphrey.FrontEnd;
using LLVMSharp.Interop;

namespace Humphrey.Backend
{
    public class CompilationValue : ICompilationValue
    {
        LLVMValueRef valueRef;
        CompilationValue storage;
        CompilationType typeRef;
        Result<Tokens> frontendLocation;
        uint alignment;

        public CompilationValue(LLVMValueRef val, CompilationType type, Result<Tokens> frontendLoc)
        {
            valueRef = val;
            typeRef = type;
            storage = null;
            frontendLocation = frontendLoc;
            alignment = 0;
        }

        public LLVMValueRef BackendValue => valueRef;
        public LLVMTypeRef BackendType => typeRef.BackendType;

        public CompilationValue Storage 
        {
            get { return storage; }
            set { storage = value; }
        }
        public CompilationType Type => typeRef;
        public uint Alignment
        {
            get { return alignment; }
            set { alignment = value; }
        }
        public Result<Tokens> FrontendLocation => frontendLocation;
    }
}
