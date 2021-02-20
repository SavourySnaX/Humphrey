using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstUnderscoreExpression : IExpression
    {
        public AstUnderscoreExpression()
        {
        }

        public string Dump()
        {
            return $"_";
        }

        public ICompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            return new CompilationConstantUndefKind(Token);
        }

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            return ProcessConstantExpression(unit);
        }

        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}




