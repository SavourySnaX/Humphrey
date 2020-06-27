using LLVMSharp.Interop;

namespace Humphrey.Backend
{
    public class CompilationFunctionType : CompilationType
    {
        private int outParameterOffset;
        public CompilationFunctionType(LLVMTypeRef type, int outParamOffset) : base(type)
        {
            outParameterOffset = outParamOffset;
        }

        public int OutParamOffset => outParameterOffset;
    }
}