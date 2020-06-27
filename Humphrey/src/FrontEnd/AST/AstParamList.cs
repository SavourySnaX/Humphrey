using System.Collections.Generic;
using System.Text;
using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstParamList : IAst
    {
        AstParamDefinition[] paramList;
        public AstParamList(AstParamDefinition[] parameters)
        {
            paramList = parameters;
        }
    
        public CompilationParam[] FetchParamList(CompilationUnit unit)
        {
            var pList = new CompilationParam[paramList.Length];

            int pIdx = 0;
            foreach(var param in paramList)
            {
                pList[pIdx++] = (param.FetchParam(unit));
            }

            return pList;
        }

        public bool Compile(CompilationUnit unit)
        {
            return false;
        }
    
        public string Dump()
        {
            var s = new StringBuilder();
            for (int a = 0; a < paramList.Length; a++)
            {
                if (a!=0)
                    s.Append(" , ");
                s.Append(paramList[a].Dump());
            }
            return s.ToString();
        }
    }
}


