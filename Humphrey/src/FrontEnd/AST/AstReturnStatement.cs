using System.Text;
using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public class AstReturnStatement : IStatement
    {
        IExpression[] exprList;
        public AstReturnStatement(IExpression[] expressionList)
        {
            exprList = expressionList;
        }

        public bool BuildStatement(CompilationUnit unit, CompilationFunction function, CompilationBuilder builder)
        {
            uint outParamIdx = function.OutParamOffset;
            foreach (var expr in exprList)
            {
                var value = expr.ProcessExpression(unit, builder);

                var parameter = function.BackendValue.GetParam(outParamIdx++);
                builder.BackendValue.BuildStore(value.BackendValue, parameter);
            }

            builder.BackendValue.BuildRetVoid();

            return true;
        }

        public bool Compile(CompilationUnit unit)
        {
            return false;
        }
    
        public string Dump()
        {
            if (exprList.Length==0)
                return "return";
            var s = new StringBuilder();
            for (int a = 0; a < exprList.Length; a++)
            {
                if (a != 0)
                    s.Append(" , ");
                s.Append(exprList[a].Dump());
            }
            return $"return {s.ToString()}";
        }
    }
}

