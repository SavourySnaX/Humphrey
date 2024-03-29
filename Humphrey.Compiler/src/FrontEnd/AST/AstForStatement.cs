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
            builder.SetDebugLocation(new SourceLocation(Token));

            if (rangeList.Length != identifiers.Length)
                throw new System.NotImplementedException($"identifiers.length != rangeList.Length");

            if (identifiers.Length != 1)
                throw new System.NotImplementedException($"Todo multiple block assignments..etc");

            for (int idx = 0; idx < identifiers.Length; idx++)
            {
                // Compute start value
                identifiers[idx].ProcessExpressionForStore(unit, builder, rangeList[idx].InclusiveStart);
            }

            //Create check_end
            var checkBlock = new CompilationBlock(unit.AppendNewBasicBlockToFunction(function,$"for_check_{identifiers[0].Dump()}"));
            var compilationBlock = loopBlock.CreateCodeBlock(unit, function, builder.LocalBuilder, $"for_block_{identifiers[0].Dump()}");
            var iterBlock = new CompilationBlock(unit.AppendNewBasicBlockToFunction(function, $"for_iter_{identifiers[0].Dump()}"));
            var endBlock = new CompilationBlock(unit.AppendNewBasicBlockToFunction(function,$"for_end_{identifiers[0].Dump()}"));

            builder.Branch(checkBlock);

            // CheckBlock performs iter end check basically
            {
                var checkBuilder = unit.CreateBuilder(function, checkBlock);
                checkBuilder.SetDebugLocation(new SourceLocation(Token));
                var compare = new AstBinaryCompareLess(identifiers[0], rangeList[0].ExclusiveEnd);
                compare.Token=rangeList[0].Token;
                var cond = compare.ProcessExpression(unit, checkBuilder);
                checkBuilder.ConditionalBranch(Expression.ResolveExpressionToValue(unit, cond, null), compilationBlock.entry, endBlock);
            }

            // insert branch at end of for_block
            {
                var loopBlockBuilder = unit.CreateBuilder(function, compilationBlock.exit);
                if (compilationBlock.exit.BackendValue.Terminator==null)
                {
                    loopBlockBuilder.SetDebugLocation(new SourceLocation(loopBlock.BlockEnd));
                    loopBlockBuilder.Branch(iterBlock);
                }
            }

            // IterBlock performs iter next
            {
                var iterBuilder = unit.CreateBuilder(function, iterBlock);
                iterBuilder.SetDebugLocation(new SourceLocation(rangeList[0].Token));
                var binaryAdd = new AstBinaryPlus(identifiers[0], new AstNumber("1"));
                identifiers[0].ProcessExpressionForStore(unit, iterBuilder, binaryAdd);
                iterBuilder.Branch(checkBlock);
            }

            // ensure our builder correctly points at end block now
            builder.PositionAtEnd(endBlock);

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

        public void Semantic(SemanticPass pass)
        {
            foreach (var i in identifiers)
            {
                i.Semantic(pass);
            }
            foreach (var r in rangeList)
            {
                r.InclusiveStart.Semantic(pass);
                r.ExclusiveEnd.Semantic(pass);
            }
            loopBlock.Semantic(pass);
        }

        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}


