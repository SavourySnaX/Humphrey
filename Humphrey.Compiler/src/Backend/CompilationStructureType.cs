using Humphrey.FrontEnd;
using LLVMSharp.Interop;

namespace Humphrey.Backend
{
    public class CompilationStructureType : CompilationType
    {
        CompilationType[] elementTypes;
        string[] elementNames;
        bool forwardDecleration;
        bool preventSameRecursion;
        public CompilationStructureType(LLVMTypeRef type, CompilationDebugBuilder debugBuilder, SourceLocation location, string ident = "") : base(type, debugBuilder, location, ident)
        {
            elementTypes = null;
            elementNames = null;
            forwardDecleration = true;
            preventSameRecursion = false;
            CreateDebugType();
        }

        public CompilationStructureType(LLVMTypeRef type, CompilationType[] elements, string[] names, CompilationDebugBuilder debugBuilder, SourceLocation location, string ident = "") : base(type, debugBuilder, location, ident)
        {
            elementTypes = elements;
            elementNames = names;
            forwardDecleration = false;
            preventSameRecursion = false;
            CreateDebugType();
        }

        public void UpdateNamedStruct(CompilationType[] elements, string[] names)
        {
            elementTypes = elements;
            elementNames = names;
            forwardDecleration = false;
            UpdateDebugType();
        }

        public bool IsSame(CompilationStructureType check)
        {
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

        public override bool Same(CompilationType obj)
        {
            if (preventSameRecursion)
                return true;

            var check = obj as CompilationStructureType;
            if (check == null)
                return false;

            preventSameRecursion = true;
            var ret = IsSame(check);
            preventSameRecursion = false;
            return ret;
        }

        public override CompilationType CopyAs(string identifier)
        {
            if (forwardDecleration)
                return new CompilationStructureType(BackendType, DebugBuilder, Location, identifier);
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
            if (elementTypes[idx]==null)
            {
                // Undefined type - error handled at source
                throw new CompilationAbortException($"Attempt to dereference an undefined type from structure '{identifier}'");
            }

            var cPtrType = unit.CreatePointerType(elementTypes[idx], elementTypes[idx].Location);
            return builder.InBoundsGEP(this, src, cPtrType, new LLVMValueRef[] { unit.CreateI32Constant(0), unit.CreateI32Constant(idx) });
        }

        void CreateDebugType()
        {
            if (DebugBuilder.Enabled)
            {
                var name = DumpType();
                CompilationDebugType debugType;
                if (forwardDecleration)
                    debugType = DebugBuilder.CreateForwardStructureType(name, this);
                else
                    debugType = DebugBuilder.CreateStructureType(name, this);
                SetDebugType(debugType);
            }
        }

        private void UpdateDebugType()
        {
            var name = DumpType();
            var debugType = DebugBuilder.CreateStructureType(name, this);
            DebugBuilder.ReplaceForwardStructWithFinal(DebugType, debugType);
            SetDebugType(debugType);
        }

        public override string DumpType()
        {
            var name = Identifier;
            if (string.IsNullOrEmpty(name))
            {
                name = "__anonymous_struct_";
                var eTs = elementTypes;
                if (eTs != null)
                {
                    foreach (var e in eTs)
                    name += $"{e.DumpType()}_";
                }
                else
                {
                    name+="frwding";
                }
            }
            return name;
        }

        public string[] Fields => elementNames;
        public CompilationType[] Elements => elementTypes;
    }
}
