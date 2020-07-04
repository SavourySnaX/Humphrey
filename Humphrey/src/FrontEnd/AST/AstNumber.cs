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
        public CompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            return new CompilationConstantValue(this);
        }

        public CompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            return unit.CreateConstant(this);
        }
    }
}