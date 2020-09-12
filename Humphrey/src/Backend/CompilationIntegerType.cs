using LLVMSharp.Interop;

namespace Humphrey.Backend
{
    public class CompilationIntegerType : CompilationType
    {
        bool signedType;
        public CompilationIntegerType(LLVMTypeRef type, bool isSigned, CompilationDebugBuilder debugBuilder, SourceLocation sourceLocation, string identifier = "") : base(type, debugBuilder, sourceLocation, identifier)
        {
            signedType = isSigned;
            CreateDebugType();
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
        public override CompilationType CopyAs(string identifier)
        {
            return new CompilationIntegerType(BackendType, signedType, DebugBuilder, Location, identifier);
        }

        void CreateDebugType()
        {
            var numBits = IntegerWidth;
            var signed = IsSigned;
            var name = Identifier;
            if (string.IsNullOrEmpty(name))
                name = $"{(signed ? "__anonymous__s" : "__anonymous__u")}{numBits}";
            var dbg = DebugBuilder.CreateBasicType(name, numBits, signed ? CompilationDebugBuilder.BasicType.SignedInt : CompilationDebugBuilder.BasicType.UnsignedInt);
            CreateDebugType(dbg);
        }
    }
}