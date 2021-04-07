using System.Text;
using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstExpressionStatement : IStatement
    {
        IExpression expression;
        public AstExpressionStatement(IExpression computeExpression)
        {
            expression = computeExpression;
        }

        public bool BuildStatement(CompilationUnit unit, CompilationFunction function, CompilationBuilder builder)
        {
            builder.SetDebugLocation(new SourceLocation(Token));
            expression.ProcessExpression(unit, builder);
            return true;
        }

        public string Dump()
        {
            return $"{expression.Dump()}";
        }

        public void Semantic(SemanticPass pass)
        {
            expression.Semantic(pass);
        }

        public IExpression Expression => expression;
        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}



