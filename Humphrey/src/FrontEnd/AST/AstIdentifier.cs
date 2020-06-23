
namespace Humphrey.FrontEnd
{
    public class AstIdentifier : IExpression,IType
    {
        string temp;
        public AstIdentifier(string value)
        {
            temp = value;
        }
    
        public string Dump()
        {
            return temp;
        }
    }
}
