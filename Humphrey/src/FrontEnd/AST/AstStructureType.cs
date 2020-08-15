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
            int numElements = 0;
            foreach (var element in definitions)
                numElements += element.NumElements;
            var elementTypes = new CompilationType[numElements];
            var names = new string[numElements];
            int idx = 0;
            foreach(var element in definitions)
            {
                for (int a = 0; a < element.NumElements; a++)
                {
                    names[idx] = element.Identifiers[a].Dump();
                    elementTypes[idx++] = element.Type.CreateOrFetchType(unit);
                }
            }

            return unit.FetchStructType(elementTypes, names);
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
        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}


