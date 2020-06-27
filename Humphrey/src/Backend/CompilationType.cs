using static Extensions.Helpers;

using LLVMSharp.Interop;

namespace Humphrey.Backend
{
    public class CompilationType
    {
        private LLVMTypeRef typeRef;
        public CompilationType(LLVMTypeRef type)
        {
            typeRef = type;
        }

        public CompilationType AsPointer()
        {
            return new CompilationType(CreatePointerType(typeRef));
        }

        public LLVMTypeRef BackendType => typeRef;
    }
}