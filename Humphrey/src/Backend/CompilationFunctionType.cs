using LLVMSharp.Interop;

namespace Humphrey.Backend
{
    public class CompilationFunctionType : CompilationType
    {
        CompilationParam[] parameters;
        private uint outParameterOffset;
        public CompilationFunctionType(LLVMTypeRef type, CompilationParam[] allParameters, uint outParamOffset) : base(type,false)
        {
            parameters = allParameters;
            outParameterOffset = outParamOffset;
        }

        public uint OutParamOffset => outParameterOffset;
        public CompilationParam[] Parameters => parameters;
    }
}