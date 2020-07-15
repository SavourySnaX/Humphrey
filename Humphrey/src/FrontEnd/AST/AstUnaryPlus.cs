using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public class AstUnaryPlus : IExpression
    {
        IExpression expr;
        public AstUnaryPlus(IExpression expression)
        {
            expr = expression;
        }
    
        public string Dump()
        {
            return $"+ {expr.Dump()}";
        }

        public CompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            // + is 0 + which is noop
            return expr.ProcessConstantExpression(unit);
        }

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            return expr.ProcessExpression(unit, builder);
        }
    }
}

