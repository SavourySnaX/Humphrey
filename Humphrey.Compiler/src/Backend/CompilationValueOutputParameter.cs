using Humphrey.FrontEnd;
using LLVMSharp.Interop;

namespace Humphrey.Backend
{
    public class CompilationValueOutputParameter : CompilationValue
    {
        private string identifier;
        public CompilationValueOutputParameter(LLVMValueRef val, CompilationType type, string paramName, Result<Tokens> token) : base(val, type, token)
        {
            identifier = paramName;
        }

        public string Identifier => identifier;
    }
}

