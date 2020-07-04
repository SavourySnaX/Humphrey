using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstBinaryModulus : IExpression
    {
        IExpression lhs;
        IExpression rhs;
        public AstBinaryModulus(IExpression left, IExpression right)
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
            return $"% {lhs.Dump()} {rhs.Dump()}";
        }

        public CompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            var valueLeft = lhs.ProcessConstantExpression(unit);
            var valueRight = rhs.ProcessConstantExpression(unit);

            valueLeft.Rem(valueRight);

            return valueLeft;
        }

        public CompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            var valueLeft = lhs.ProcessExpression(unit, builder);
            var valueRight = rhs.ProcessExpression(unit, builder);

            var result = builder.BackendValue.BuildURem(valueLeft.BackendValue, valueRight.BackendValue);
            
            return new CompilationValue(result);
        }
    }
}




