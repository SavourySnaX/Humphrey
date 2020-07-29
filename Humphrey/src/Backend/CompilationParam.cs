using Humphrey.FrontEnd;

namespace Humphrey.Backend
{
    public class CompilationParam
    {
        private CompilationType type;
        private AstIdentifier identifier;

        public CompilationParam(CompilationType itype, AstIdentifier iidentifier)
        {
            type = itype;
            identifier = iidentifier;
        }

        public CompilationType Type => type;

        public string Identifier => identifier.Dump();

        public Result<Tokens> Token => identifier.Token;
    }
}