using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public class AstArrayType : IType
    {
        IType elementType;
        IExpression constantExpression;
        public AstArrayType(IExpression arraySize, IType type)
        {
            elementType = type;
            constantExpression = arraySize;
        }

        public (CompilationType compilationType, IType originalType) CreateOrFetchType(CompilationUnit unit)
        {
            var exprValue = constantExpression.ProcessConstantExpression(unit) as CompilationConstantIntegerKind;

            var isBit = elementType as AstBitType;
            if (isBit==null)
            {
                var elementCompilationType = elementType.CreateOrFetchType(unit).compilationType;
                if (elementCompilationType == null)
                {
                    // return a fake array type to allow compilation to continue
                    elementCompilationType = new AstBitType().CreateOrFetchType(unit).compilationType;
                }
                return (unit.FetchArrayType(exprValue, elementCompilationType, new SourceLocation(elementType.Token)), this);
            }
            else
                return (unit.FetchIntegerType(exprValue, new SourceLocation(elementType.Token)), this);
        }

        public bool IsFunctionType => elementType.IsFunctionType;
    
        public string Dump()
        {
            return $"[{constantExpression.Dump()}] {elementType.Dump()}";
        }

        public void Semantic(SemanticPass pass)
        {
            constantExpression.Semantic(pass);
            elementType.Semantic(pass);
        }

        public IType ResolveBaseType(SemanticPass pass)
        {
            return this;    // or element type?
        }

        public IType ElementType => elementType;
        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

        private AstMetaData metaData;
        public AstMetaData MetaData { get => metaData; set => metaData = value; }

        public SemanticPass.IdentifierKind GetBaseType => elementType.GetBaseType;
    }
}
