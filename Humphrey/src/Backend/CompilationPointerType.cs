using LLVMSharp.Interop;

namespace Humphrey.Backend
{
    public class CompilationPointerType : CompilationType
    {
        CompilationType element;
        public CompilationPointerType(LLVMTypeRef type, CompilationType elementType) : base(type)
        {
            element = elementType;
        }

        public CompilationType ElementType => element;
        public override bool Same(CompilationType obj)
        {
            var check = obj as CompilationPointerType;
            if (check == null)
                return false;
            return  Identifier == check.Identifier && element.Same(check.element);
        }
    }
}
