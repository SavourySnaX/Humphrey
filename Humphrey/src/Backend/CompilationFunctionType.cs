using LLVMSharp.Interop;

namespace Humphrey.Backend
{
    public class CompilationFunctionType : CompilationType
    {
        CompilationParam[] parameters;
        private uint outParameterOffset;
        public CompilationFunctionType(LLVMTypeRef type, CompilationParam[] allParameters, uint outParamOffset) : base(type)
        {
            parameters = allParameters;
            outParameterOffset = outParamOffset;
        }

        public uint OutParamOffset => outParameterOffset;
        public CompilationParam[] Parameters => parameters;

        public override bool Same(CompilationType obj)
        {
            var check = obj as CompilationFunctionType;
            if (check == null)
                return false;

            if (parameters.Length!=check.parameters.Length)
                return false;
            for (int a = 0; a < parameters.Length;a++)
            {
                if (!parameters[a].Type.Same(check.parameters[a].Type))
                    return false;
            }
            return outParameterOffset == check.outParameterOffset && Identifier == check.Identifier;
        }

    }
}