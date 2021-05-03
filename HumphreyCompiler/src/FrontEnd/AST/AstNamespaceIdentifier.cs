using System.Text;
using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstNamespaceIdentifier : IExpression,IType,ILoadValue,IStorable, IIdentifier, ISymbolScope
    {
        IIdentifier[] names;
        IIdentifier[] fullPath;
        CommonSymbolTable symbolTable;  // end, ie the table that contains final
        IIdentifier final;
        //private bool semanticDone;
        public AstNamespaceIdentifier(IIdentifier[] items)
        {
            fullPath = items;
            names = items[0..^1];
            final = items[^1];
            //semanticDone = false;
        }
    
        public (CompilationType compilationType, IType originalType) CreateOrFetchType(CompilationUnit unit)
        {
            var recoverTo = unit.PushNamespaceScope(symbolTable);
            var result = unit.FetchNamedType(this);
            unit.PopNamespaceScope(recoverTo);
            return result;
        }
    
        public bool IsFunctionType => false;
    
        public string Dump()
        {
            var s = new StringBuilder();
            for (int a = 0; a < names.Length; a++)
            {
                if (a!=0)
                    s.Append("::");
                s.Append(names[a].Name);
            }
            s.Append($"::{final.Name}");
            return s.ToString();
        }

        public ICompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            throw new System.NotImplementedException($"Todo implement constant expression processing for constant values");
        }

        public (CommonSymbolTable, CommonSymbolTable) PushNamespace(CompilationUnit unit)
        {
            return unit.PushNamespaceScope(symbolTable);
        }

        public void PopNamespace(CompilationUnit unit, (CommonSymbolTable,CommonSymbolTable) recoverTo)
        {
            unit.PopNamespaceScope(recoverTo);
        }

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            var recoverTo = unit.PushNamespaceScope(symbolTable);
            var result = unit.FetchValue(this, builder);
            unit.PopNamespaceScope(recoverTo);
            return result;
        }

        public void ProcessExpressionForStore(CompilationUnit unit, CompilationBuilder builder, IExpression value)
        {
            throw new System.NotImplementedException($"TODO");
            /*
            var storeTo = unit.FetchLocation(name, builder);
            if (storeTo.Type is CompilationPointerType ptrType)
            {
                CompilationType elementType = ptrType.ElementType;
                var storeValue = AstUnaryExpression.EnsureTypeOk(unit, builder, value, elementType);
                builder.Store(storeValue, storeTo);
            }
            else
            {
                throw new System.NotImplementedException($"Cannot store value to type");
            }*/
        }

        public IType ResolveExpressionType(SemanticPass pass)
        {
            var typeOfValue = pass.ResolveValueType(this);
            if (typeOfValue==null)
            {
                return this;    // This identifier refers to a type
            }
            return typeOfValue;
        }

        public void Semantic(SemanticPass pass)
        {
            ResolveBaseType(pass);
            /*
            if (!semanticDone)
            {
                semanticDone = true;
                if (!pass.AddSemanticLocation(this, Token))
                {
                    pass.Messages.Log(CompilerErrorKind.Error_UndefinedValue, $"Type '{Name}' is not found in the current scope.", Token.Location, Token.Remainder);
                }
            }
            */
        }

        public IType ResolveBaseType(SemanticPass pass)
        {
            // We need to fetch the symbol table for the namespace
            var recover = pass.PushNamespace(names);
            symbolTable = recover.root;

            var resolvedFinal = final as IType;

            var resolveBase = resolvedFinal.ResolveBaseType(pass);

            pass.PopNamespace(recover.recoverTo);

            return resolveBase;
        }

        public string Name => final.Name;

        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

        private AstMetaData metaData;
        public AstMetaData MetaData { get => metaData; set => metaData = value; }

        public SemanticPass.IdentifierKind GetBaseType => SemanticPass.IdentifierKind.None;

        public CommonSymbolTable SymbolTable => symbolTable;
        public IIdentifier[] FullPath => fullPath;
    }
}


