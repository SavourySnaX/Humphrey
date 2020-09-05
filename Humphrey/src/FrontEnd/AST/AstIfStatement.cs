using System.Text;
using Humphrey.Backend;
using LLVMSharp.Interop;

namespace Humphrey.FrontEnd
{
    public class AstIfStatement : IStatement
    {
        IExpression condition;
        AstCodeBlock conditionTrue;
        AstCodeBlock conditionElse;
        public AstIfStatement(IExpression expression, AstCodeBlock ifBlock, AstCodeBlock elseBlock)
        {
            condition = expression;
            conditionTrue = ifBlock;
            conditionElse = elseBlock;
        }

        public bool BuildStatement(CompilationUnit unit, CompilationFunction function, CompilationBuilder builder)
        {
            // Create blocks for if/end and else
            var trueBlock = conditionTrue.CreateCodeBlock(unit, function, "if_if");
            var endBlock = new CompilationBlock(function.BackendValue.AppendBasicBlock($"if_end"));

            // Evaluate condition
            // TODO need to validate we have a true/false result type 
            var condTest = condition.ProcessExpression(unit, builder);
            var resolved = Expression.ResolveExpressionToValue(unit, condTest, null);

            // Insert branch at end of trueBlock
            {
                var endCondBuilder = unit.CreateBuilder(function, trueBlock.exit);
                endCondBuilder.Branch(endBlock);
            }

            // Jump to correct block
            if (conditionElse == null)
            {
                builder.ConditionalBranch(resolved, trueBlock.entry, endBlock);
            }
            else
            {
                var falseBlock = conditionElse.CreateCodeBlock(unit, function, "if_else");

                builder.ConditionalBranch(resolved, trueBlock.entry, falseBlock.entry);
            
                // Insert branch at end of elseBlock
                {
                    var endCondBuilder = unit.CreateBuilder(function, falseBlock.exit);
                    endCondBuilder.Branch(endBlock);
                }
            }

            // ensure our builder correctly points at end block now
            builder.PositionAtEnd(endBlock);

            return true;
        }

        public string Dump()
        {
            var s = new StringBuilder();

            s.Append($"if {condition.Dump()} {conditionTrue.Dump()}");
            if (conditionElse!=null)
                s.Append($" else {conditionElse.Dump()}");
            return s.ToString();
        }

        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}


