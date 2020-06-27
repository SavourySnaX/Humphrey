using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public class AstUnaryExpression : IExpression
    {
        IOperator op;
        IExpression expr;
        public AstUnaryExpression(IOperator oper, IExpression expression)
        {
            op = oper;
            expr = expression;
        }
    
        public bool Compile(CompilationUnit unit)
        {
            return false;
        }
    
        public string Dump()
        {
            return $"{op.Dump()} {expr.Dump()}";
        }

        public CompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            throw new System.NotImplementedException();
        }
    }
}


