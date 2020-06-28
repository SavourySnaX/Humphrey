using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public class AstUnaryMinus : IExpression
    {
        IExpression expr;
        public AstUnaryMinus(IExpression expression)
        {
            expr = expression;
        }
    
        public bool Compile(CompilationUnit unit)
        {
            return false;
        }
    
        public string Dump()
        {
            return $"- {expr.Dump()}";
        }

        public CompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            throw new System.NotImplementedException();
        }
    }
}
