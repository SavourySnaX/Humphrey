
namespace Humphrey.FrontEnd
{
    public class AstNumber : IExpression
    {
        string temp;
        public AstNumber(string value)
        {
            temp = value;
        }
    
        public string Dump()
        {
            return temp;
        }
    }
}