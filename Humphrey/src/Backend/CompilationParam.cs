namespace Humphrey.Backend
{
    public class CompilationParam
    {
        private CompilationType type;
        private string identifier;

        public CompilationParam(CompilationType itype, string iidentifier)
        {
            type = itype;
            identifier = iidentifier;
        }

        public CompilationType Type => type;
    }
}