using LLVMSharp.Interop;

namespace Humphrey.Backend
{
    public class CompilationFloatType : CompilationType
    {
        public CompilationFloatType(LLVMTypeRef type, CompilationDebugBuilder debugBuilder, SourceLocation sourceLocation, string identifier = "") : base(type, debugBuilder, sourceLocation, identifier)
        {
            CreateDebugType();
        }

        public override bool Same(CompilationType obj)
        {
            var check = obj as CompilationFloatType;
            if (check == null)
                return false;

            return Identifier == "" || check.Identifier == "" || Identifier == check.Identifier;
        }

        public override CompilationType CopyAs(string identifier)
        {
            return new CompilationFloatType(BackendType, DebugBuilder, Location, identifier);
        }

        void CreateDebugType()
        {
            if (DebugBuilder.Enabled)
            {
                var name = DumpType();
                var dbg = DebugBuilder.CreateBasicType(name, 32, CompilationDebugBuilder.BasicType.Float);
                SetDebugType(dbg);
            }
        }

        public override string DumpType()
        {
            var name = Identifier;
            if (string.IsNullOrEmpty(name))
                name = $"__anonymous__fp32";
            return name;
        }

    }
}
