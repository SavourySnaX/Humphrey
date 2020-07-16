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

    }
}
