using System.Text;
using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public class AstStructureType : IType
    {
        AstStructElement[] definitions;
        bool semanticDone;
        public AstStructureType(AstStructElement[] defList, bool autoTyped = false)
        {
            definitions = defList;
            semanticDone = autoTyped;
        }
    
        public (CompilationType compilationType, IType originalType) CreateOrFetchType(CompilationUnit unit)
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
                    var current = element.Type.CreateOrFetchType(unit).compilationType;
                    if (current is CompilationFunctionType compilationFunctionType)
                    {
                        current = unit.CreatePointerType(compilationFunctionType, compilationFunctionType.Location);
                    }
                    elementTypes[idx++] = current;
                }
            }

            return (unit.FetchStructType(elementTypes, names, new SourceLocation(Token)), this);
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

        public void Semantic(SemanticPass pass)
        {
            if (!semanticDone)
            {
                semanticDone = true;
                foreach (var d in definitions)
                {
                    d.Semantic(pass);
                }
            }
        }

        public IType ResolveBaseType(SemanticPass pass)
        {
            return this;
        }

        public AstStructElement[] Elements => definitions;
        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

        private AstMetaData metaData;
        public AstMetaData MetaData { get => metaData; set => metaData = value; }

        public SemanticPass.IdentifierKind GetBaseType => SemanticPass.IdentifierKind.StructType;
    }
}


