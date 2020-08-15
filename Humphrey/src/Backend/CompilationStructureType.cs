using Humphrey.FrontEnd;
using LLVMSharp.Interop;

namespace Humphrey.Backend
{
    public class CompilationStructureType : CompilationType
    {
        CompilationType[] elementTypes;
        string[] elementNames;
        public CompilationStructureType(LLVMTypeRef type, CompilationType[] elements, string[] names) : base(type)
        {
            elementTypes = elements;
            elementNames = names;
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
            var clone = new CompilationStructureType(BackendType, elementTypes, elementNames);
            clone.identifier = identifier;
            return clone;
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

            var resultPtrType = Extensions.Helpers.CreatePointerType(elementTypes[idx].BackendType);
            var cPtrType = new CompilationPointerType(resultPtrType, elementTypes[idx]);
            return builder.InBoundsGEP(src, cPtrType, new LLVMValueRef[] { unit.CreateI32Constant(0), unit.CreateI32Constant(idx) });
        }

        public string[] Fields => elementNames;
    }
}
