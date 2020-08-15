using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstBinaryAs : IExpression
    {
        IExpression lhs;
        IType rhs;
        public AstBinaryAs(IExpression left, IType right)
        {
            lhs = left;
            rhs = right;
        }
    
        public string Dump()
        {
            return $"as {lhs.Dump()} {rhs.Dump()}";
        }

        public CompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            var valueLeft = lhs.ProcessConstantExpression(unit);
            valueLeft.Cast(rhs);
            return valueLeft;
        }

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            var vlhs = lhs.ProcessExpression(unit, builder);
            if (vlhs is CompilationConstantValue)
                return ProcessConstantExpression(unit);

            var valueLeft = vlhs as CompilationValue;
            var typeRight = rhs.CreateOrFetchType(unit);

            if (valueLeft.Type.Same(typeRight))
                return valueLeft;

            return builder.Cast(valueLeft, typeRight);
        }
        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}




