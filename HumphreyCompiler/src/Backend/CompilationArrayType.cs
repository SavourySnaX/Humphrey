using LLVMSharp.Interop;

namespace Humphrey.Backend
{
    public class CompilationArrayType : CompilationType
    {
        CompilationType element;
        uint elementCount;
        public CompilationArrayType(LLVMTypeRef type, CompilationType elementType, uint numElements, CompilationDebugBuilder debugBuilder, SourceLocation location, string ident = "") : base(type, debugBuilder, location, ident)
        {
            element = elementType;
            elementCount = numElements;
            CreateDebugType();
        }

        public override bool Same(CompilationType obj)
        {
            var check = obj as CompilationArrayType;
            if (check == null)
                return false;
            return elementCount == check.elementCount && Identifier == check.Identifier && element.Same(check.element);
        }

        public override CompilationType CopyAs(string identifier)
        {
            return  new CompilationArrayType(BackendType, element, elementCount, DebugBuilder, Location, identifier);
        }

        void CreateDebugType()
        {
            if (DebugBuilder.Enabled)
            {
                var name = DumpType();
                var dbg = DebugBuilder.CreateArrayType(name, this);
                SetDebugType(dbg);
            }
        }

        public override string DumpType()
        {
            if (string.IsNullOrEmpty(Identifier))
                return $"__anonymous__array_{ElementType.DumpType()}_{elementCount}";
            return Identifier;
        }

        public CompilationType ElementType => element;
        public uint ElementCount => elementCount;
    }
}

