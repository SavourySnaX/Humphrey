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

        public CompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            return new CompilationConstantValue();
        }

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            return new CompilationConstantValue();
        }
    }
}



