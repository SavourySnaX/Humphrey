using System.Text;
namespace Humphrey.FrontEnd
{
    public class AstExpressionList : IAst
    {
        IExpression[] expressions;

        public AstExpressionList()
        {
            expressions = new IExpression[] { };
        }

        public AstExpressionList(IExpression[] expressionList)
        {
            expressions = expressionList;
        }

        public string Dump()
        {
            if (expressions.Length==0)
                return "";
            var s = new StringBuilder();
            for (int a = 0; a < expressions.Length; a++)
            {
                if (a != 0)
                    s.Append(" , ");
                s.Append(expressions[a].Dump());
            }
            return s.ToString();
        }

        public IExpression[] Expressions => expressions;
        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}



