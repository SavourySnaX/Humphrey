using System.Text;
using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstWhileStatement : IStatement
    {
        IExpression condition;
        AstCodeBlock loop;
        public AstWhileStatement(IExpression expression, AstCodeBlock whileBlock)
        {
            condition = expression;
            loop = whileBlock;
        }

        public bool BuildStatement(CompilationUnit unit, CompilationFunction function, CompilationBuilder builder)
        {
            builder.SetDebugLocation(new SourceLocation(Token));

            //Create check_end
            var checkBlock = new CompilationBlock(function.BackendValue.AppendBasicBlock($"while_check"));
            var compilationBlock = loop.CreateCodeBlock(unit, function, builder.LocalBuilder, $"while_block");
            var endBlock = new CompilationBlock(function.BackendValue.AppendBasicBlock($"while_end"));

            builder.Branch(checkBlock);

            // CheckBlock 
            {
                var checkBuilder = unit.CreateBuilder(function, checkBlock);
                checkBuilder.LocalBuilder = builder.LocalBuilder;
                var cond = AstUnaryExpression.EnsureTypeOk(unit, checkBuilder, condition, unit.CreateIntegerType(1, false, new SourceLocation(Token)));
                checkBuilder.ConditionalBranch(Expression.ResolveExpressionToValue(unit, cond, null), compilationBlock.entry, endBlock);
            }

            // insert branch at end of while_block (if block is not already terminated)
            {
                var loopBlockBuilder = unit.CreateBuilder(function, compilationBlock.exit);
                if (compilationBlock.exit.BackendValue.Terminator == null)
                {
                    loopBlockBuilder.Branch(checkBlock);
                }
            }

            // ensure our builder correctly points at end block now
            builder.PositionAtEnd(endBlock);

            return true;
        }

        public string Dump()
        {
            return $"while {condition.Dump()} {loop.Dump()}";
        }

        public void Semantic(SemanticPass pass)
        {
            condition.Semantic(pass);
            loop.Semantic(pass);
        }

        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}



