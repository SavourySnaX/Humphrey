using LLVMSharp.Interop;

namespace Humphrey.Backend
{
    public class CompilationValueOutputParameter : CompilationValue
    {
        private string identifier;
        public CompilationValueOutputParameter(LLVMValueRef val, CompilationType type, string paramName) : base(val, type)
        {
            identifier = paramName;
        }

        public string Identifier => identifier;
    }
}

