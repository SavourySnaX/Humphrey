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

        public bool HasOutputs => outParameterOffset < Parameters.Length;

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

        public CompilationStructureType CreateOutputParameterStruct(CompilationUnit unit)
        {
            if (HasOutputs)
            {
                var types = new CompilationType[Parameters.Length - outParameterOffset];
                var names = new string[Parameters.Length - outParameterOffset];
                for (uint a = outParameterOffset; a < Parameters.Length; a++)
                {
                    types[a - outParameterOffset] = Parameters[a].Type;
                    names[a - outParameterOffset] = Parameters[a].Identifier;
                }
                return unit.FetchStructType(types, names) as CompilationStructureType;
            }

            return null;
        }

        public override CompilationType CopyAs(string identifier)
        {
            var clone = new CompilationFunctionType(BackendType, parameters, OutParamOffset);
            clone.identifier = identifier;
            return clone;
        }

        public long InputCount => Parameters.Length - (Parameters.Length - outParameterOffset);
    }
}