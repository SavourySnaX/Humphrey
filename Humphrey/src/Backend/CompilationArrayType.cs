using LLVMSharp.Interop;

namespace Humphrey.Backend
{
    public class CompilationArrayType : CompilationType
    {
        CompilationType element;
        uint elementCount;
        public CompilationArrayType(LLVMTypeRef type, CompilationType elementType, uint numElements) : base(type)
        {
            element = elementType;
            elementCount = numElements;
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
            var clone = new CompilationArrayType(BackendType, element, elementCount);
            clone.identifier = identifier;
            return clone;
        }

        public CompilationType ElementType => element;
    }
}

