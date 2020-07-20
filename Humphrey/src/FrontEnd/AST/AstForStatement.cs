using System.Text;
using Humphrey.Backend;
using LLVMSharp.Interop;

namespace Humphrey.FrontEnd
{
    public class AstForStatement : IStatement
    {
        AstLoadableIdentifier[] identifiers;
        AstRange[] rangeList;
        AstCodeBlock loopBlock;
        public AstForStatement(AstLoadableIdentifier[] identList, AstRange[] ranges, AstCodeBlock block)
        {
            rangeList = ranges;
            identifiers = identList;
            loopBlock = block;
        }

        public bool BuildStatement(CompilationUnit unit, CompilationFunction function, CompilationBuilder builder)
        {
            if (rangeList.Length != identifiers.Length)
                throw new System.NotImplementedException($"identifiers.length != rangeList.Length");

            if (identifiers.Length != 1)
                throw new System.NotImplementedException($"Todo nultiple block assignments..etc");

            for (int idx = 0; idx < identifiers.Length; idx++)
            {
                // Compute start value
                identifiers[idx].ProcessExpressionForStore(unit, builder, rangeList[idx].InclusiveStart);
            }

            //Create check_end
            var checkBlock = new CompilationBlock(function.BackendValue.AppendBasicBlock($"for_check_{identifiers[0].Dump()}"));
            var compilationBlock = loopBlock.CreateCodeBlock(unit, function, $"for_block_{identifiers[0].Dump()}");
            var iterBlock = new CompilationBlock(function.BackendValue.AppendBasicBlock($"for_iter_{identifiers[0].Dump()}"));
            var endBlock = new CompilationBlock(function.BackendValue.AppendBasicBlock($"for_end_{identifiers[0].Dump()}"));

            builder.BackendValue.BuildBr(checkBlock.BackendValue);

            // CheckBlock performs iter end check basically
            {
                var checkBuilder = unit.CreateBuilder(function, checkBlock);
                var loadVal = identifiers[0].ProcessExpression(unit, checkBuilder);
                var end = rangeList[0].ExclusiveEnd.ProcessExpression(unit, checkBuilder);
                var cond = checkBuilder.BackendValue.BuildICmp(LLVMIntPredicate.LLVMIntULT, Expression.ResolveExpressionToValue(unit, loadVal, null).BackendValue, Expression.ResolveExpressionToValue(unit, end, null).BackendValue);
                checkBuilder.BackendValue.BuildCondBr(cond, compilationBlock.BackendValue, endBlock.BackendValue);
            }

            // insert branch at end of for_block
            {
                var loopBlockBuilder = unit.CreateBuilder(function, compilationBlock);
                loopBlockBuilder.BackendValue.BuildBr(iterBlock.BackendValue);
            }

            // IterBlock performs iter next
            {
                var iterBuilder = unit.CreateBuilder(function, iterBlock);
                var binaryAdd = new AstBinaryPlus(identifiers[0], new AstNumber("1"));
                identifiers[0].ProcessExpressionForStore(unit, iterBuilder, binaryAdd);
                iterBuilder.BackendValue.BuildBr(checkBlock.BackendValue);
            }

            // ensure our builder correctly points at end block now
            builder.BackendValue.PositionAtEnd(endBlock.BackendValue);


            //throw new System.NotImplementedException($"Todo for statement");
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


