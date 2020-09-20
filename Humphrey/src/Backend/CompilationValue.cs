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

        public CompilationValue(LLVMValueRef val, CompilationType type, Result<Tokens> frontendLoc)
        {
            valueRef = val;
            typeRef = type;
            storage = null;
            frontendLocation = frontendLoc;
        }

        public LLVMValueRef BackendValue => valueRef;
        public LLVMTypeRef BackendType => typeRef.BackendType;

        public CompilationValue Storage 
        {
            get { return storage; }
            set { storage = value; }
        }
        public CompilationType Type => typeRef;

        public Result<Tokens> FrontendLocation => frontendLocation;
    }
}
