using LLVMSharp.Interop;

namespace Humphrey.Backend
{
    public class CompilationIntegerType : CompilationType
    {
        bool signedType;
        public CompilationIntegerType(LLVMTypeRef type, bool isSigned) : base(type)
        {
            signedType = isSigned;
        }

        public uint IntegerWidth => BackendType.IntWidth;

        public bool IsSigned => signedType;

        public override bool Same(CompilationType obj)
        {
            var check = obj as CompilationIntegerType;
            if (check == null)
                return false;
            return signedType == check.signedType && Identifier == check.Identifier && IntegerWidth == check.IntegerWidth;
        }
    }
}