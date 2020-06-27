using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public class AstKeyword : IAst
    {
        string temp;
        public AstKeyword(string value)
        {
            temp = value;
        }
    
        public bool Compile(CompilationUnit unit)
        {
            return false;
        }
    
        public string Dump()
        {
            return temp;
        }
    }
}
