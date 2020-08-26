using Humphrey.Backend;

using static Extensions.Helpers;

namespace Humphrey.FrontEnd
{
    public class AstUnaryPreDecrement : IExpression
    {
        IExpression expr;
        public AstUnaryPreDecrement(IExpression expression)
        {
            expr = expression;
        }
    
        public string Dump()
        {
            return $"-- {expr.Dump()}";
        }

        public CompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            var result = expr.ProcessConstantExpression(unit);
            result.Decrement();
            return result;
        }

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            var value = expr.ProcessExpression(unit, builder);
            if (value is CompilationConstantValue constantValue)
            {
                constantValue.Decrement();
                return constantValue;
            }
            else
            {
                var cv = value as CompilationValue;
                var decremented = cv;
                var decByType = cv.Type;
                if (cv.Type is CompilationIntegerType)
                {
                    var decBy = new CompilationValue(decByType.BackendType.CreateConstantValue(1), decByType);
                    decremented = builder.Sub(cv, decBy);
                }
                else if (cv.Type is CompilationPointerType cpt)
                {
                    // GEP
                    decremented = builder.InBoundsGEP(cv, cpt, new LLVMSharp.Interop.LLVMValueRef[] { unit.CreateI64Constant(0xFFFFFFFFFFFFFFFF) });
                }
                else
                {
                    throw new System.NotImplementedException($"pre decrement on unsupported type {decByType}");
                }
                builder.Store(decremented, cv.Storage);
                return decremented;
            }
        }
        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}
