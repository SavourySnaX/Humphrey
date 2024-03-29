using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public class AstUnaryBinaryNot : IExpression
    {
        IExpression expr;
        public AstUnaryBinaryNot(IExpression expression)
        {
            expr = expression;
        }
    
        public string Dump()
        {
            return $"~ {expr.Dump()}";
        }

        public ICompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            var result = expr.ProcessConstantExpression(unit) as CompilationConstantIntegerKind;
            result.Not();
            return result;
        }

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            var value = expr.ProcessExpression(unit, builder);
            if (value is CompilationConstantIntegerKind constantValue)
            {
                constantValue.Not();
                return constantValue;
            }
            else
            {
                var cv = value as CompilationValue;
                if (cv.Type is CompilationIntegerType || cv.Type is CompilationEnumType)
                {
                    return builder.Not(cv);
                }
                else
                {
                    unit.Messages.Log(CompilerErrorKind.Error_ExpectedType, $"Expected an integer", Token.Location, Token.Remainder);
                    return cv;
                }
            }
        }

        public IType ResolveExpressionType(SemanticPass pass)
        {
            return expr.ResolveExpressionType(pass);
        }

        public void Semantic(SemanticPass pass)
        {
            expr.Semantic(pass);
        }

        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}

