using System.Collections.Generic;
using System.Text;
using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public class AstAliasType : IType
    {
        IType type;
        AstStructElement[][] definitions;
        private bool semanticDone;
        public AstAliasType(IType enumType, AstStructElement[][] defList)
        {
            type = enumType;
            definitions = defList;
            semanticDone = false;
        }
    
        public (CompilationType compilationType, IType originalType) CreateOrFetchType(CompilationUnit unit)
        {
            var baseKind = type.CreateOrFetchType(unit);
            if (!VerifyAliasIsValid(unit, baseKind))
            {
                var cTypes = new CompilationType[0][];
                var names = new string[0][];
                var rotate = new uint[0][];

                return (unit.FetchAliasType(baseKind.compilationType, cTypes, names, rotate, new SourceLocation(Token)), this);
            }
            else
            {

                var cTypes = new CompilationType[definitions.Length][];
                var names = new string[definitions.Length][];
                var rotate = new uint[definitions.Length][];

                for (int a = 0; a < definitions.Length; a++)
                {
                    int numElements = 0;
                    foreach (var element in definitions[a])
                        numElements += element.NumElements;
                    cTypes[a] = new CompilationType[numElements];
                    names[a] = new string[numElements];
                    rotate[a] = new uint[numElements];

                    uint start = (baseKind.compilationType as CompilationIntegerType).IntegerWidth;
                    uint idx = 0;
                    for (int b = 0; b < definitions[a].Length; b++)
                    {
                        for (int c = 0; c < definitions[a][b].NumElements; c++)
                        {
                            cTypes[a][idx] = definitions[a][b].Type.CreateOrFetchType(unit).compilationType;
                            names[a][idx] = definitions[a][b].Identifiers[c].Name;
                            start -= (cTypes[a][idx] as CompilationIntegerType).IntegerWidth;
                            rotate[a][idx] = start;
                            idx++;
                        }
                    }
                }

                // For now, just create the base type (debugability be damned)- TODO - figure out how to map the elements for debug information
                return (unit.FetchAliasType(baseKind.compilationType, cTypes, names, rotate, new SourceLocation(Token)), this);
            }
        }
    
        public bool IsFunctionType => false;

        public IEnumerable<AstStructElement> Elements {
            get
            {
                foreach (var elements in definitions)
                {
                    foreach (var element in elements)
                    {
                        yield return element;
                    }
                }
            }
        }

        public string Dump()
        {
            var s = new StringBuilder();
            s.Append($"{type.Dump()} ");
            for (int b=0;b<definitions.Length;b++)
            {
                var elements = definitions[b];
                if (b != 0)
                    s.Append(" ");
                s.Append("|{ ");
                for (int a = 0; a < elements.Length; a++)
                {
                    if (a != 0)
                        s.Append(" ");
                    s.Append(elements[a].Dump());
                }
                s.Append("}");
            }
            return s.ToString();
        }

        public void Semantic(SemanticPass pass)
        {
            if (!semanticDone)
            {
                semanticDone = true;
                type.Semantic(pass);
                var checkUnique=new HashSet<string>();
                checkUnique.Add("raw");                     // Builtin
                foreach (var elements in definitions)
                {
                    foreach (var d in elements)
                    {
                        foreach (var n in d.Identifiers)
                        {
                            if (n.Name != "_")
                            {
                                if (checkUnique.Contains(n.Name))
                                {
                                    pass.Messages.Log(CompilerErrorKind.Error_DuplicateSymbol, "Duplicate symbol defined in alias definition : {i.Name}", n.Token.Location, n.Token.Remainder);
                                }
                                else
                                    checkUnique.Add(n.Name);
                            }
                        }
                        d.Semantic(pass);
                    }
                }
            }
        }

        // Extra verification for aliases :
        // alias must be same size as parent (gets complex for structs....)
        // alias must not cross a type memory boundary
        //e.g.
        //  Struct with 2 members UInt8 UInt8
        //  Alias must be 16 bits long, and a single alias member must not cross between the two elements in the parent struct
        private bool VerifyAliasIsValid(CompilationUnit unit, (CompilationType compilationType, IType originalType) cType)
        {
            if (cType.compilationType is CompilationIntegerType integerType)
            {
                var width = integerType.IntegerWidth;

                foreach (var elements in definitions)
                {
                    uint compareWidth = 0;
                    foreach (var element in elements)
                    {
                        var t = element.Type.CreateOrFetchType(unit);// We already do this elsewhere, so wasteful
                        
                        if (t.compilationType is CompilationIntegerType cIT)
                        {
                            compareWidth+=cIT.IntegerWidth;
                        }
                        else
                        {
                            throw new System.NotImplementedException($"TODO - need support for aliasing non integer types!");
                        }
                    }

                    if (compareWidth!=width)
                    {
                        unit.Messages.Log(CompilerErrorKind.Error_AliasWidthMismatch, $"Alias widths must match base type! BaseType Width : {width} != {compareWidth}", type.Token.Location, type.Token.Remainder);
                        return false;
                    }
                }
            }
            else
            {
                throw new System.NotImplementedException($"TODO - need support for aliasing non integer types!");
            }
            return true;
        }


        public IType ResolveBaseType(SemanticPass pass)
        {
            return this;
        }

        public IType RawType => type;
        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

        private AstMetaData metaData;
        public AstMetaData MetaData { get => metaData; set => metaData = value; }

        public SemanticPass.IdentifierKind GetBaseType => SemanticPass.IdentifierKind.AliasType;
    }
}



