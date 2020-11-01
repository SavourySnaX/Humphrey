using System.Text;
using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstMetaData : IAst
    {
        AstIdentifier[] names;
        public AstMetaData(AstIdentifier[] value)
        {
            names = value;
        }
    
        public bool Contains(string contains)
        {
            foreach(var ident in names)
            {
                if (ident.Name == contains)
                    return true;
            }
            return false;
        }

        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

        public string Dump()
        {
            var s = new StringBuilder();
            s.Append("[ ");
            for (int a = 0; a < names.Length; a++)
            {
                if (a!=0)
                    s.Append(" , ");
                s.Append(names[a].Dump());
            }
            s.Append(" ]");
            return s.ToString();
        }
    }
}

