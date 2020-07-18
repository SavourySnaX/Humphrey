using System.Text;
using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public class AstArraySubscript : IStatement,IExpression,ILoadValue
    {
        IExpression expr;
        IExpression subscriptIdx;
        public AstArraySubscript(IExpression expression, IExpression subscript)
        {
            subscriptIdx = subscript;
            expr = expression;
        }

        public bool BuildStatement(CompilationUnit unit, CompilationFunction function, CompilationBuilder builder)
        {
            throw new System.NotImplementedException($"ArraySubscript TODO");
            /*uint outParamIdx = function.OutParamOffset;
            foreach (var expr in exprList)
            {
                var paramType = function.FunctionType.Parameters[outParamIdx].Type;
                var value = AstUnaryExpression.EnsureTypeOk(unit, builder, expr, paramType);

                var parameter = function.BackendValue.GetParam(outParamIdx++);

                builder.BackendValue.BuildStore(value.BackendValue, parameter);
            }

            builder.BackendValue.BuildRetVoid();

            return true;*/
        }

        public string Dump()
        {
            return $"{expr.Dump()} [ {subscriptIdx.Dump()} ]";
        }

        public CompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            throw new System.NotImplementedException($"Todo implement constant expression for subscript....");
        }

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            throw new System.NotImplementedException($"Todo implement expression for subscript...");
        }
    }
}



