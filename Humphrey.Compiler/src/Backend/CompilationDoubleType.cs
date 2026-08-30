using LLVMSharp.Interop;

namespace Humphrey.Backend
{
    public class CompilationDoubleType : CompilationType
    {
        public CompilationDoubleType(LLVMTypeRef type, CompilationDebugBuilder debugBuilder, SourceLocation sourceLocation, string identifier = "") : base(type, debugBuilder, sourceLocation, identifier)
        {
            CreateDebugType();
        }

        public override bool Same(CompilationType obj)
        {
            var check = obj as CompilationDoubleType;
            if (check == null)
                return false;

            return Identifier == "" || check.Identifier == "" || Identifier == check.Identifier;
        }

        public override CompilationType CopyAs(string identifier)
        {
            return new CompilationDoubleType(BackendType, DebugBuilder, Location, identifier);
        }

        void CreateDebugType()
        {
            if (DebugBuilder.Enabled)
            {
                var name = DumpType();
                var dbg = DebugBuilder.CreateBasicType(name, 64, CompilationDebugBuilder.BasicType.Float);
                SetDebugType(dbg);
            }
        }

        public override string DumpType()
        {
            var name = Identifier;
            if (string.IsNullOrEmpty(name))
                name = $"__anonymous__fp64";
            return name;
        }
    }
}
