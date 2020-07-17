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

    }
}
