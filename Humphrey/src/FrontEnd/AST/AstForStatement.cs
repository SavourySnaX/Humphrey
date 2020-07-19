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
            //for loop
            //
            //
            // initialise vars (can be done on current location)
            // br check_end
            //
            // check_end
            //  load initial var
            //  cmp range
            //  if < jmp for_block
            //  else jmp_end
            //
            // jmp_end:
            //  (update current builder bb to point here)
            //
            // for_block:
            //  ...
            //  next_iter  (for now just ++ var)
            //  br check_end


            if (rangeList.Length != identifiers.Length)
                throw new System.NotImplementedException($"identifiers.length != rangeList.Length");
    
            if (identifiers.Length!=1)
                throw new System.NotImplementedException($"Todo nultiple block assignments..etc");

            for (int idx = 0; idx < identifiers.Length; idx++)
            {
                // Compute start value
                identifiers[idx].ProcessExpressionForStore(unit, builder, rangeList[idx].InclusiveStart);
            }

            //Create check_end
            var checkEndBlock = new CompilationBlock(function.BackendValue.AppendBasicBlock($"for_check_end_{identifiers[0].Dump()}"));
            var endBlock = new CompilationBlock(function.BackendValue.AppendBasicBlock($"for_end_{identifiers[0].Dump()}"));

            var compilationBlock = loopBlock.CreateCodeBlock(unit, function, $"for_block_{identifiers[0].Dump()}");

            builder.BackendValue.BuildBr(checkEndBlock.BackendValue);

            var checkEndBuilder= unit.CreateBuilder(function, checkEndBlock);

            var loadVal = identifiers[0].ProcessExpression(unit, checkEndBuilder);
            var end = rangeList[0].ExclusiveEnd.ProcessExpression(unit, checkEndBuilder);
            var cond= checkEndBuilder.BackendValue.BuildICmp(LLVMIntPredicate.LLVMIntULT, Expression.ResolveExpressionToValue(unit, loadVal, null).BackendValue, Expression.ResolveExpressionToValue(unit, end, null).BackendValue);
            checkEndBuilder.BackendValue.BuildCondBr(cond, compilationBlock.BackendValue, endBlock.BackendValue);

            var loopBlockBuilder = unit.CreateBuilder(function, compilationBlock);
            loopBlockBuilder.BackendValue.BuildBr(checkEndBlock.BackendValue);

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


