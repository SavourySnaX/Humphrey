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
    
        public string Dump()
        {
            return temp;
        }
        public ICompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            return new CompilationConstantIntegerKind(this);
        }

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            return new CompilationConstantIntegerKind(this);
        }
        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}