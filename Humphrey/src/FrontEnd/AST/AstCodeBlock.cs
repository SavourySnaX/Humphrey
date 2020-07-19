
using System.Text;
using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstCodeBlock : IAssignable, IStatement
    {
        IStatement[] statementList;
        public AstCodeBlock(IStatement[] statements)
        {
            statementList = statements;
        }
    
        public CompilationBlock CreateCodeBlock(CompilationUnit unit, CompilationFunction function, string blockName)
        {
            var newBB = new CompilationBlock(function.BackendValue.AppendBasicBlock(blockName));

            var builder = unit.CreateBuilder(function, newBB);

            foreach (var s in statementList)
            {
                s.BuildStatement(unit, function, builder);
            }

            return newBB;
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
    }
}

