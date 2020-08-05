using Humphrey.FrontEnd;
using LLVMSharp.Interop;

namespace Humphrey.Backend
{
    public class CompilationStructureType : CompilationType
    {
        CompilationType[] elementTypes;
        public CompilationStructureType(LLVMTypeRef type, CompilationType[] elements) : base(type)
        {
            elementTypes = elements;
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
            }
            return Identifier == check.Identifier;
        }

        public CompilationValue LoadElement(CompilationUnit unit, CompilationBuilder builder, CompilationValue src, string identifier)
        {
            // Find identifier in elements
            uint idx=0;
            foreach (var i in elementTypes)
            {
                if (i.Identifier==identifier)
                    break;
                idx++;
            }

            return builder.ExtractValue(src,elementTypes[idx], idx);
        }

        public void StoreElement(CompilationUnit unit, CompilationBuilder builder, CompilationValue dst, IExpression src, string identifier)
        {
            // Find identifier in elements
            uint idx=0;
            foreach (var i in elementTypes)
            {
                if (i.Identifier==identifier)
                    break;
                idx++;
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
            foreach (var i in elementTypes)
            {
                if (i.Identifier==identifier)
                    break;
                idx++;
            }

            var resultPtrType = Extensions.Helpers.CreatePointerType(elementTypes[idx].BackendType);
            var cPtrType = new CompilationPointerType(resultPtrType, elementTypes[idx]);
            return builder.InBoundsGEP(src, cPtrType, new LLVMValueRef[] { unit.CreateI32Constant(0), unit.CreateI32Constant(idx) });
        }

        public CompilationType[] Elements => elementTypes;
    }
}
