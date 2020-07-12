using System.Text;
using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public class AstPointerType : IType
    {
        IType elementType;
        public AstPointerType(IType element)
        {
            elementType = element;
        }
    
        public CompilationType CreateOrFetchType(CompilationUnit unit)
        {
            var ct = elementType.CreateOrFetchType(unit);
            return ct.AsPointer();
        }
    
        public bool Compile(CompilationUnit unit)
        {
            return false;
        }

        public bool IsFunctionType => false;

        public string Dump()
        {
            return $"* {elementType.Dump()}";
        }

        public IType ElementType => elementType;
    }
}



