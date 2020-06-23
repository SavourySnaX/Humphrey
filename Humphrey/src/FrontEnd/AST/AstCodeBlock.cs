
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Humphrey.FrontEnd
{
    public class AstCodeBlock : IAssignable, IStatement
    {
        IStatement[] statementList;
        public AstCodeBlock(IStatement[] statements)
        {
            statementList = statements;
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
    }
}

