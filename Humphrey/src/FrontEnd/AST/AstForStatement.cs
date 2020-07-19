using System.Text;
using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public class AstForStatement : IStatement
    {
        AstIdentifier[] identifiers;
        AstRange[] rangeList;
        AstCodeBlock loopBlock;
        public AstForStatement(AstIdentifier[] identList, AstRange[] ranges, AstCodeBlock block)
        {
            rangeList = ranges;
            identifiers = identList;
            loopBlock = block;
        }

        public bool BuildStatement(CompilationUnit unit, CompilationFunction function, CompilationBuilder builder)
        {
            throw new System.NotImplementedException($"Todo for statement");
            /*uint outParamIdx = function.OutParamOffset;
            foreach (var expr in exprList.Expressions)
            {
                var paramType = function.FunctionType.Parameters[outParamIdx].Type;
                var value = AstUnaryExpression.EnsureTypeOk(unit, builder, expr, paramType);

                var parameter = function.BackendValue.GetParam(outParamIdx++);

                builder.BackendValue.BuildStore(value.BackendValue, parameter);
            }

            builder.BackendValue.BuildRetVoid();
*/
            return true;
        }

        public string Dump()
        {
            var s = new StringBuilder();

            s.Append("for ");
            int idx = 0;
            foreach (var ident in identifiers)
            {
                // TODO eventually we want to be able to have , or * seperation (for crazyness)
                if (idx!=0)
                    s.Append(" , ");
                s.Append(ident.Dump());
                idx++;
            }
            s.Append(" = ");    // TODO := to allow creating of variables instead of assignments
            idx = 0;
            foreach (var range in rangeList)
            {
                if (idx!=0)
                    s.Append(" , ");
                s.Append(range.Dump());
                idx++;
            }
            s.Append($" {loopBlock.Dump()}");
            return s.ToString();
        }
    }
}


