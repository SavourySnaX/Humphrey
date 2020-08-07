using LLVMSharp.Interop;

namespace Humphrey.Backend
{
    public abstract class CompilationType
    {
        protected string identifier;
        LLVMTypeRef typeRef;

        public CompilationType(LLVMTypeRef type)
        {
            typeRef = type;
            identifier = "";
        }

        public abstract CompilationType CopyAs(string identifier);

        public LLVMTypeRef BackendType => typeRef;

        public abstract bool Same(CompilationType t);
        public string Identifier => identifier;
    }
}
