using LLVMSharp.Interop;

namespace Humphrey.Backend
{
    public abstract class CompilationType
    {
        string identifier;
        LLVMTypeRef typeRef;

        public CompilationType(LLVMTypeRef type)
        {
            typeRef = type;
            identifier = "";
        }

        public LLVMTypeRef BackendType => typeRef;

        public abstract bool Same(CompilationType t);
        public string Identifier { get { return identifier; } set { identifier = value; } }
    }
}
