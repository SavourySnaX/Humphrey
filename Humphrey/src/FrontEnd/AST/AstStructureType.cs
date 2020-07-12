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
/*            var inputs = inputList.FetchParamList(unit);
            var outputs = outputList.FetchParamList(unit);

            return unit.CreateFunctionType(inputs, outputs);
*/
            throw new System.NotImplementedException($"Unimplemented Type create/fetch");
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
    }
}


