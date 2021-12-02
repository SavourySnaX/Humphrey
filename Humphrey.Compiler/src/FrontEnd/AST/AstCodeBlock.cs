
using System.Text;
using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstCodeBlock : IAssignable, IStatement, ISymbolScope
    {
        IStatement[] statementList;
        public AstCodeBlock(IStatement[] statements)
        {
            statementList = statements;
        }
    
        public (CompilationBlock entry, CompilationBlock exit) CreateCodeBlock(CompilationUnit unit, CompilationFunction function, CompilationBuilder locals, string blockName)
        {
            var oldScope = unit.PushScope(symbolTable, unit.CreateDebugScope(new SourceLocation(Token)));
            var newBB = new CompilationBlock(unit.AppendNewBasicBlockToFunction(function,blockName));

            var builder = unit.CreateBuilder(function, newBB);
            builder.LocalBuilder = locals;

            foreach (var s in statementList)
            {
                s.BuildStatement(unit, function, builder);
            }

            unit.PopScope(oldScope);
            return (newBB, builder.CurrentBlock);
        }
    
        public string Dump()
        {
            var s = new StringBuilder();

            s.Append("{ ");
            foreach(var statement in statementList)
            {
                s.Append($"{statement.Dump()}");
            }
            s.Append("}");

            return s.ToString();
        }

        public bool BuildStatement(CompilationUnit unit, CompilationFunction function, CompilationBuilder builder)
        {
            throw new System.NotImplementedException();
        }

        public void Semantic(SemanticPass pass)
        {
            symbolTable = pass.PushScope();
            foreach (var statement in statementList)
            {
                statement.Semantic(pass);
            }
            pass.PopScope();
        }

        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

        public Result<Tokens> BlockStart;
        public Result<Tokens> BlockEnd;

        public IStatement[] Statements => statementList;

        private CommonSymbolTable symbolTable;
        public CommonSymbolTable SymbolTable => symbolTable;
    }
}

