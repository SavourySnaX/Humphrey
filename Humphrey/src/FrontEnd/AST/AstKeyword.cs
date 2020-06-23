namespace Humphrey.FrontEnd
{
    public class AstKeyword : IAst
    {
        string temp;
        public AstKeyword(string value)
        {
            temp = value;
        }
    
        public string Dump()
        {
            return temp;
        }
    }
}
