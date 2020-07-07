using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstBinaryPlus : IExpression
    {
        IExpression lhs;
        IExpression rhs;
        public AstBinaryPlus(IExpression left, IExpression right)
        {
            lhs = left;
            rhs = right;
        }
    
        public bool Compile(CompilationUnit unit)
        {
            return false;
        }

        public string Dump()
        {
            return $"+ {lhs.Dump()} {rhs.Dump()}";
        }

        public CompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            var valueLeft = lhs.ProcessConstantExpression(unit);
            var valueRight = rhs.ProcessConstantExpression(unit);

            valueLeft.Add(valueRight);

            return valueLeft;
        }

        public CompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            var (valueLeft, valueRight) = AstBinaryExpression.FixupBinaryExpressionInputs(unit, builder, lhs, rhs);

            return builder.Add(valueLeft, valueRight);
        }
    }
}


