using Humphrey.Backend;
using static Extensions.Helpers;

namespace Humphrey.FrontEnd
{
    public class AstUnaryPostIncrement : IExpression
    {
        IExpression expr;
        public AstUnaryPostIncrement(IExpression expression)
        {
            expr = expression;
        }
    
        public string Dump()
        {
            return $"({expr.Dump()})++";
        }

        public CompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            var result = expr.ProcessConstantExpression(unit);
            result.Increment();
            return result;
        }

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            var value = expr.ProcessExpression(unit, builder);
            if (value is CompilationConstantValue constantValue)
            {
                constantValue.Increment();
                return constantValue;
            }
            else
            {
                var cv = value as CompilationValue;
                var incremented = cv;
                var incByType = cv.Type;
                if (cv.Type is CompilationIntegerType)
                {
                    var incBy = new CompilationValue(incByType.BackendType.CreateConstantValue(1), incByType, Token);
                    incremented = builder.Add(cv, incBy);
                }
                else if (cv.Type is CompilationPointerType cpt)
                {
                    // GEP
                    incremented = builder.InBoundsGEP(cv, cpt, new LLVMSharp.Interop.LLVMValueRef[] { unit.CreateI64Constant(1) });
                }
                else
                {
                    throw new System.NotImplementedException($"post increment on unsupported type {incByType}");
                }
                builder.Store(incremented, cv.Storage);
                return cv;
            }
        }

        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}
