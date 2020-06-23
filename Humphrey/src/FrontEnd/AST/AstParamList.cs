using System.Text;

namespace Humphrey.FrontEnd
{
    public class AstParamList : IAst
    {
        AstParamDefinition[] paramList;
        public AstParamList(AstParamDefinition[] parameters)
        {
            paramList = parameters;
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


