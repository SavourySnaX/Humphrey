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
            builder.SetDebugLocation(new SourceLocation(Token));

            // Create blocks for if/end and else
            var trueBlock = conditionTrue.CreateCodeBlock(unit, function, builder.LocalBuilder, "if_if");
            var endBlock = new CompilationBlock(unit.AppendNewBasicBlockToFunction(function, $"if_end"));

            // Evaluate condition
            // TODO need to validate we have a true/false result type 
            var condTest = AstUnaryExpression.EnsureTypeOk(unit, builder, condition, unit.CreateIntegerType(1, false, new SourceLocation(Token)));
            var resolved = Expression.ResolveExpressionToValue(unit, condTest, null);

            // Insert branch at end of trueBlock
            {
                var endCondBuilder = unit.CreateBuilder(function, trueBlock.exit);
                if (trueBlock.exit.BackendValue.Terminator == null)
                {
                    endCondBuilder.SetDebugLocation(new SourceLocation(conditionTrue.BlockEnd));
                    endCondBuilder.Branch(endBlock);
                }
            }

            // Jump to correct block
            if (conditionElse == null)
            {
                builder.ConditionalBranch(resolved, trueBlock.entry, endBlock);
            }
            else
            {
                var falseBlock = conditionElse.CreateCodeBlock(unit, function, builder.LocalBuilder, "if_else");

                builder.ConditionalBranch(resolved, trueBlock.entry, falseBlock.entry);
            
                // Insert branch at end of elseBlock
                {
                    var endCondBuilder = unit.CreateBuilder(function, falseBlock.exit);
                    if (falseBlock.exit.BackendValue.Terminator == null)
                    {
                        endCondBuilder.SetDebugLocation(new SourceLocation(conditionElse.BlockEnd));
                        endCondBuilder.Branch(endBlock);
                    }
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

        public void Semantic(SemanticPass pass)
        {
            condition.Semantic(pass);
            conditionTrue.Semantic(pass);
            conditionElse?.Semantic(pass);
        }

        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}


