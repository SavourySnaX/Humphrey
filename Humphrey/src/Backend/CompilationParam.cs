using Humphrey.FrontEnd;
using LLVMSharp.Interop;

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
        public CompilationDebugType DebugType => type.DebugType;

        public string Identifier => identifier.Dump();

        public Result<Tokens> Token => identifier.Token;
    }
}