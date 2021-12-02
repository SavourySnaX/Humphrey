using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstLoadableIdentifier : IExpression,IType,ILoadValue,IStorable, IIdentifier
    {
        string name;
        private bool semanticDone;
        public AstLoadableIdentifier(string value)
        {
            name = value;
            semanticDone = false;
        }
    
        public (CompilationType compilationType, IType originalType) CreateOrFetchType(CompilationUnit unit)
        {
            return unit.FetchNamedType(this);
        }
    
        public bool IsFunctionType => false;
    
        public string Dump()
        {
            return name;
        }
        public ICompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            throw new System.NotImplementedException($"Todo implement constant expression processing for constant values");
        }

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            return unit.FetchValue(this, builder);
        }

        public void ProcessExpressionForStore(CompilationUnit unit, CompilationBuilder builder, IExpression value)
        {
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
            }
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
            if (!semanticDone)
            {
                semanticDone = true;
                if (!pass.AddSemanticLocation(this, Token))
                {
                    pass.Messages.Log(CompilerErrorKind.Error_UndefinedValue, $"Type '{Name}' is not found in the current scope.", Token.Location, Token.Remainder);
                }
            }
        }

        public IType ResolveBaseType(SemanticPass pass)
        {
            var resolved = pass.FetchNamedType(this);
            if (resolved==null)
            {
                return this;    // Error should have already been caught
            }
            return resolved.ResolveBaseType(pass);
        }

        public string Name => name;

        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

        private AstMetaData metaData;
        public AstMetaData MetaData { get => metaData; set => metaData = value; }

        public SemanticPass.IdentifierKind GetBaseType => SemanticPass.IdentifierKind.None;
    }
}

