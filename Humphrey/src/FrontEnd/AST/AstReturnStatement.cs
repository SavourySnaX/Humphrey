using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public class AstReturnStatement : IStatement
    {
        AstExpressionList exprList;
        public AstReturnStatement(AstExpressionList expressionList)
        {
            exprList = expressionList;
        }

        public bool BuildStatement(CompilationUnit unit, CompilationFunction function, CompilationBuilder builder)
        {
            uint outParamIdx = function.OutParamOffset;
            foreach (var expr in exprList.Expressions)
            {
                var paramType = function.FunctionType.Parameters[outParamIdx].Type;
                var value = AstUnaryExpression.EnsureTypeOk(unit, builder, expr, paramType);

                var parameter = function.BackendValue.GetParam(outParamIdx++);

                builder.BackendValue.BuildStore(value.BackendValue, parameter);
            }

            builder.BackendValue.BuildRetVoid();

            return true;
        }

        public string Dump()
        {
            if (exprList.Expressions.Length==0)
                return "return";
            return $"return {exprList.Dump()}";
        }
    }
}

