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
    
        public string Dump()
        {
            return $"{op.Dump()} {expr.Dump()}";
        }
    }
}


