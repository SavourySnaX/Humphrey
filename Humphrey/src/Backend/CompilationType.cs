using static Extensions.Helpers;

using LLVMSharp.Interop;

namespace Humphrey.Backend
{
    public class CompilationType
    {
        bool signedType;
        bool functionType;
        private LLVMTypeRef typeRef;
        public CompilationType(LLVMTypeRef type, bool isSigned, bool isFunction)
        {
            typeRef = type;
            signedType = isSigned;
            functionType = isFunction;
        }

        public CompilationType AsPointer()
        {
            return new CompilationType(CreatePointerType(typeRef), false, false);
        }

        public CompilationType AsArray(uint numElements)
        {
            return new CompilationType(CreateArrayType(typeRef, numElements), signedType, functionType);
        }

        public bool IsIntegerType => typeRef.Kind == LLVMTypeKind.LLVMIntegerTypeKind;

        public uint IntegerWidth => typeRef.IntWidth;

        public LLVMTypeRef BackendType => typeRef;
        public bool IsSigned => signedType;
        public bool IsFunctionType => functionType;
    }
}