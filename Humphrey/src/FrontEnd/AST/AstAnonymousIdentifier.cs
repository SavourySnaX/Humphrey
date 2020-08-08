using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstAnonymousIdentifier : IIdentifier
    {
        public AstAnonymousIdentifier()
        {
        }
    
        public string Dump()
        {
            return "_";
        }

        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}
