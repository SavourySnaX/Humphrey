using System.Collections.Generic;
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
    
        public (CompilationType compilationType, IType originalType) CreateOrFetchType(CompilationUnit unit)
        {
            // An enum, is essentially a constant array of constant values
            //Indexed by name rather than integer

            var enumType = type.CreateOrFetchType(unit).compilationType;
            var values = new CompilationConstantValue[definitions.Length];
            var names = new Dictionary<string, uint>();
            uint idx = 0;
            foreach(var element in definitions)
            {
                values[idx] = element.ProcessConstantExpression(unit);
                for (int a = 0; a < element.NumElements; a++)
                {
                    names[element.Identifiers[a].Dump()] = idx;
                }
                idx++;
            }

            return (unit.FetchEnumType(enumType, values, names, new SourceLocation(Token)), this);
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


