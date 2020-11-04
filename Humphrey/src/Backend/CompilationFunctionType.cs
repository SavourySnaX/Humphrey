using LLVMSharp.Interop;

namespace Humphrey.Backend
{

    public class CompilationFunctionType : CompilationType
    {
        public enum CallingConvention
        {
            HumphreyInternal,
            HumphreyExternal,
            CDecl,
        }
        CallingConvention callingConvention;
        CompilationParam[] parameters;
        CompilationParam returnType;
        private uint outParameterOffset;
        private bool evaluatedDirectly;
        public CompilationFunctionType(LLVMTypeRef type, CallingConvention callConvention, CompilationParam realReturnType, CompilationParam[] allParameters, uint outParamOffset, CompilationDebugBuilder debugBuilder, SourceLocation location, string ident = "") : base(type, debugBuilder, location, ident)
        {
            parameters = allParameters;
            outParameterOffset = outParamOffset;
            returnType = realReturnType;
            callingConvention = callConvention;
            evaluatedDirectly = false;
            CreateDebugType();
        }

        public uint OutParamOffset => outParameterOffset;
        public CompilationParam[] Parameters => parameters;

        public bool HasOutputs => outParameterOffset < Parameters.Length;

        public void SetCompileTimeOnly() { evaluatedDirectly = true; }

        public bool CompileTimeOnly => evaluatedDirectly;
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

        public CompilationStructureType CreateOutputParameterStruct(CompilationUnit unit, SourceLocation location)
        {
            if (HasOutputs)
            {
                var types = new CompilationType[Parameters.Length - outParameterOffset];
                var names = new string[Parameters.Length - outParameterOffset];
                for (uint a = outParameterOffset; a < Parameters.Length; a++)
                {
                    types[a - outParameterOffset] = Parameters[a].Type;
                    names[a - outParameterOffset] = Parameters[a].Identifier.Dump();
                }
                return unit.FetchStructType(types, names, location);
            }

            return null;
        }

        public override CompilationType CopyAs(string identifier)
        {
            return new CompilationFunctionType(BackendType, callingConvention, returnType, parameters, OutParamOffset, DebugBuilder, Location, identifier);
        }

        void CreateDebugType()
        {
            if (DebugBuilder.Enabled)
            {
                var ptypes = new CompilationDebugType[parameters.Length];
                int idx = 0;
                foreach (var t in parameters)
                    ptypes[idx++] = t.DebugType;
                var name = DumpType();
                var dbg = DebugBuilder.CreateFunctionType(name, returnType == null ? null : returnType.DebugType, ptypes, Location);
                CreateDebugType(dbg);
            }
        }

        public override string DumpType()
        {
            var name = Identifier;
            if (string.IsNullOrEmpty(name))
            {
                name = "__anonymous__function__";
                foreach (var param in parameters)
                    name += $"{param.Identifier}_";
            }
            return name;
        }

        public CompilationParam ReturnType => returnType;
        public CallingConvention FunctionCallingConvention => callingConvention;
        public long InputCount => Parameters.Length - (Parameters.Length - outParameterOffset);
    }
}