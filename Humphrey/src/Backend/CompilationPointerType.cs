using LLVMSharp.Interop;

namespace Humphrey.Backend
{
    public class CompilationPointerType : CompilationType
    {
        CompilationType element;
        public CompilationPointerType(LLVMTypeRef type, CompilationType elementType, CompilationDebugBuilder debugBuilder, SourceLocation location, string identifier = "") : base(type, debugBuilder, location, identifier)
        {
            element = elementType;
            CreateDebugType();
        }

        public CompilationType ElementType => element;
        public override bool Same(CompilationType obj)
        {
            var check = obj as CompilationPointerType;
            if (check == null)
                return false;
            return  Identifier == check.Identifier && element.Same(check.element);
        }
        
        public override CompilationType CopyAs(string identifier)
        {
            return new CompilationPointerType(BackendType, element, DebugBuilder, Location, identifier);
        }

        void CreateDebugType()
        {
            var name = Identifier;
            if (string.IsNullOrEmpty(name))
                name = $"__anonymous__ptr__{element.DebugType.Identifier}";
            var dbg = DebugBuilder.CreatePointerType(name, element.DebugType);
            CreateDebugType(dbg);
        }
    }
}
