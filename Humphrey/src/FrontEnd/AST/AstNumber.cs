using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstNumber : IExpression
    {
        string temp;
        public AstNumber(string value)
        {
            temp = value;
        }
    
        public bool Compile(CompilationUnit unit)
        {
            return false;
        }
    
        public string Dump()
        {
            return temp;
        }

        public CompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            return unit.CreateConstant(temp);
        }
    }
}