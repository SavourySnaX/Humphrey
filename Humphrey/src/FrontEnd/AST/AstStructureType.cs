using System.Text;
using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public class AstStructureType : IType
    {
        AstStructElement[] definitions;
        public AstStructureType(AstStructElement[] defList)
        {
            definitions = defList;
        }
    
        public CompilationType CreateOrFetchType(CompilationUnit unit)
        {
            var elementTypes = new CompilationType[definitions.Length];
            int idx = 0;
            foreach(var element in definitions)
                elementTypes[idx++] = element.Type.CreateOrFetchType(unit);

            return unit.FetchStructType(elementTypes);
        }
    
        public bool Compile(CompilationUnit unit)
        {
            return false;
        }

        public bool IsFunctionType => false;

        public string Dump()
        {
            var s = new StringBuilder();
            s.Append("{ ");
            for (int a = 0; a < definitions.Length; a++)
            {
                if (a!=0)
                    s.Append(" ");
                s.Append(definitions[a].Dump());
            }
            s.Append("}");
            return s.ToString();
        }

        public AstStructElement[] Elements => definitions;
    }
}


