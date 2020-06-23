namespace Humphrey.FrontEnd
{
    public class AstReturnStatement : IStatement
    {
        IExpression expr;
        public AstReturnStatement(IExpression expression)
        {
            expr = expression;
        }
    
        public string Dump()
        {
            if (expr==null)
                return "return";
            return $"return {expr.Dump()}";
        }
    }
}

