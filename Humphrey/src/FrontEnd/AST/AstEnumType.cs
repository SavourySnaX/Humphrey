using System.Text;
using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public class AstEnumType : IType
    {
        IType type;
        AstEnumElement[] definitions;
        public AstEnumType(IType enumType, AstEnumElement[] defList)
        {
            type = enumType;
            definitions = defList;
        }
    
        public CompilationType CreateOrFetchType(CompilationUnit unit)
        {
            throw new System.Exception($"TODO");
            /*
            int numElements = 0;
            foreach (var element in definitions)
                numElements += element.NumElements;
            var elementTypes = new CompilationType[numElements];
            int idx = 0;
            foreach(var element in definitions)
            {
                for (int a = 0; a < element.NumElements; a++)
                {
                    var type = element.Type.CreateOrFetchType(unit).CopyAs(element.Identifiers[a].Dump());
                    elementTypes[idx++] = type;
                }
            }

            return unit.FetchStructType(elementTypes);
            */
        }
    
        public bool IsFunctionType => false;

        public string Dump()
        {
            var s = new StringBuilder();
            s.Append($"{type.Dump()} ");
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

        public AstEnumElement[] Elements => definitions;
        public IType Type => type;
        
        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}


