using System.Collections.Generic;
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

        public void CreateOrFetchNamedStruct(CompilationUnit unit, AstIdentifier[] identifiers)
        {
            var compTypes = new CompilationStructureType[identifiers.Length];
            // Create forwarding named structs
            for (int a = 0; a < identifiers.Length; a++)
            {
                var ident = identifiers[a];
                var ct = unit.CreateNamedStruct(ident.Name, new SourceLocation(Token));
                unit.CreateNamedType(ident.Name, ct, this);
                compTypes[a] = ct;
            }

            // Now create elements
            (var elementTypes, var names) = CreateElements(unit);

            // Now update our created types with the elements
            for (int a = 0; a < identifiers.Length; a++)
            {
                // Update our symbol too
                var symbol = unit.FetchNamedType(identifiers[a]).compilationType as CompilationStructureType;
                symbol.UpdateNamedStruct(elementTypes, names);
                unit.FinaliseStruct(compTypes[a], elementTypes, names);
            }
        }

        public (CompilationType compilationType, IType originalType) CreateOrFetchType(CompilationUnit unit)
        {
            (var elementTypes, var names) = CreateElements(unit);

            return (unit.FetchStructType(elementTypes, names, new SourceLocation(Token)), this);
        }

        private (CompilationType[] elementTypes, string[] names) CreateElements(CompilationUnit unit)
        {
            int numElements = 0;
            foreach (var element in definitions)
                numElements += element.NumElements;
            var elementTypes = new CompilationType[numElements];
            var names = new string[numElements];
            int idx = 0;
            foreach (var element in definitions)
            {
                for (int a = 0; a < element.NumElements; a++)
                {
                    names[idx] = element.Identifiers[a].Name;
                    var current = element.Type.CreateOrFetchType(unit).compilationType;
                    if (current is CompilationFunctionType compilationFunctionType)
                    {
                        current = unit.CreatePointerType(compilationFunctionType, compilationFunctionType.Location);
                    }
                    elementTypes[idx++] = current;
                }
            }
            return (elementTypes, names);
        }

        public bool IsFunctionType => false;

        public string Dump()
        {
            var s = new StringBuilder();
            s.Append("{ ");
            for (int a = 0; a < definitions.Length; a++)
            {
                if (a != 0)
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
                var checkUnique = new HashSet<string>();
                foreach (var d in definitions)
                {
                    foreach (var n in d.Identifiers)
                    {
                        if (n.Name != "_")
                        {
                            if (checkUnique.Contains(n.Name))
                            {
                                pass.Messages.Log(CompilerErrorKind.Error_DuplicateSymbol, "Duplicate symbol defined in struct definition : {i.Name}", n.Token.Location, n.Token.Remainder);
                            }
                            else
                                checkUnique.Add(n.Name);
                        }
                    }
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


