using Humphrey.FrontEnd;
using LLVMSharp.Interop;

namespace Humphrey.Backend
{
    public class CompilationStructureType : CompilationType
    {
        CompilationType[] elementTypes;
        string[] elementNames;
        public CompilationStructureType(LLVMTypeRef type, CompilationType[] elements, string[] names, CompilationDebugBuilder debugBuilder, SourceLocation location, string ident = "") : base(type, debugBuilder, location, ident)
        {
            elementTypes = elements;
            elementNames = names;
            CreateDebugType();
        }

        public override bool Same(CompilationType obj)
        {
            var check = obj as CompilationStructureType;
            if (check == null)
                return false;

            if (elementTypes.Length!=check.elementTypes.Length)
                return false;
            for (int a = 0; a < elementTypes.Length;a++)
            {
                if (!elementTypes[a].Same(check.elementTypes[a]))
                    return false;
                if (elementNames[a]!=check.elementNames[a])
                    return false;
            }
            return Identifier == check.Identifier;
        }

        public override CompilationType CopyAs(string identifier)
        {
            return new CompilationStructureType(BackendType, elementTypes, elementNames, DebugBuilder, Location, identifier);
        }

        public CompilationValue LoadElement(CompilationUnit unit, CompilationBuilder builder, CompilationValue src, string identifier)
        {
            // Find identifier in elements
            uint idx=0;
            foreach (var i in elementNames)
            {
                if (i == identifier)
                    break;
                idx++;
            }
            if (idx==elementTypes.Length)
            {
                // Compilation error, struct xxx does not contain field yyy
                throw new System.Exception($"Need error message and partial recovery -struct does not contain field {identifier}");
            }

            return builder.ExtractValue(src,elementTypes[idx], idx);
        }

        public void StoreElement(CompilationUnit unit, CompilationBuilder builder, CompilationValue dst, IExpression src, string identifier)
        {
            // Find identifier in elements
            uint idx=0;
            foreach (var i in elementNames)
            {
                if (i == identifier)
                    break;
                idx++;
            }
            if (idx==elementTypes.Length)
            {
                // Compilation error, struct xxx does not contain field yyy
                throw new System.Exception($"Need error message and partial recovery -struct does not contain field {identifier}");
            }

            CompilationType elementType = elementTypes[idx];
            var storeValue = AstUnaryExpression.EnsureTypeOk(unit, builder, src, elementType);

            var newVal = builder.InsertValue(dst, storeValue, idx);

            builder.Store(newVal, dst.Storage);
        }

        public CompilationValue AddressElement(CompilationUnit unit, CompilationBuilder builder, CompilationValue src, string identifier)
        {
            // Find identifier in elements
            uint idx=0;
            foreach (var i in elementNames)
            {
                if (i == identifier)
                    break;
                idx++;
            }
            if (idx==elementTypes.Length)
            {
                // Compilation error, struct xxx does not contain field yyy
                throw new System.Exception($"Need error message and partial recovery -struct does not contain field {identifier}");
            }

            var cPtrType = unit.CreatePointerType(elementTypes[idx], elementTypes[idx].Location);
            return builder.InBoundsGEP(src, cPtrType, new LLVMValueRef[] { unit.CreateI32Constant(0), unit.CreateI32Constant(idx) });
        }

        void CreateDebugType()
        {
            if (DebugBuilder.Enabled)
            {
                var name = DumpType();
                var dbg = DebugBuilder.CreateStructureType(name, this);
                CreateDebugType(dbg);
            }
        }

        public override string DumpType()
        {
            var name = Identifier;
            if (string.IsNullOrEmpty(name))
            {
                name = "__anonymous_struct_";
                foreach (var e in elementTypes)
                    name += $"{e.DumpType()}_";
            }
            return name;
        }

        public string[] Fields => elementNames;
        public CompilationType[] Elements => elementTypes;
    }
}
