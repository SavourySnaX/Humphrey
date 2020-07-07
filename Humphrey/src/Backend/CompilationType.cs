using static Extensions.Helpers;

using LLVMSharp.Interop;

namespace Humphrey.Backend
{
    public class CompilationType
    {
        bool signedType;
        private LLVMTypeRef typeRef;
        public CompilationType(LLVMTypeRef type, bool isSigned)
        {
            typeRef = type;
            signedType = isSigned;
        }

        public CompilationType AsPointer()
        {
            return new CompilationType(CreatePointerType(typeRef), false);
        }

        public CompilationType AsArray(uint numElements)
        {
            return new CompilationType(CreateArrayType(typeRef, numElements), signedType);
        }

        public bool IsIntegerType => typeRef.Kind == LLVMTypeKind.LLVMIntegerTypeKind;

        public uint IntegerWidth => typeRef.IntWidth;

        public LLVMTypeRef BackendType => typeRef;
        public bool IsSigned => signedType;
    }
}