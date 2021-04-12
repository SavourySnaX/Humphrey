using Humphrey.FrontEnd;
using LLVMSharp.Interop;

namespace Humphrey.Backend
{
    public abstract class CompilationType
    {
        CompilationDebugBuilder builderRef;
        string identifier;
        LLVMTypeRef typeRef;
        CompilationDebugType debugTypeRef;

        SourceLocation sourceLocation;

        public CompilationType(LLVMTypeRef type, CompilationDebugBuilder dbgBuilder, SourceLocation location, string ident)
        {
            typeRef = type;
            identifier = ident;
            builderRef = dbgBuilder;
            sourceLocation = location;
        }

        protected void SetDebugType(CompilationDebugType type)
        {
            debugTypeRef = type;
        }

        public abstract CompilationType CopyAs(string identifier);
        public abstract string DumpType();

        protected CompilationDebugBuilder DebugBuilder => builderRef;
        public LLVMTypeRef BackendType => typeRef;
        public CompilationDebugType DebugType => debugTypeRef;
        public SourceLocation Location => sourceLocation;
        public Result<Tokens> FrontendLocation => sourceLocation.FrontendLocation;
        public abstract bool Same(CompilationType t);
        public string Identifier => identifier;
    }
}
