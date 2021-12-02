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
    
        public string Dump()
        {
            return temp;
        }
        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}
