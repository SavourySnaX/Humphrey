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
    
        public CompilationType CreateOrFetchType(CompilationUnit unit)
        {
            var exprValue = constantExpression.ProcessConstantExpression(unit);

            var isBit = elementType as AstBitType;
            if (isBit==null)
            {
                return unit.FetchArrayType(exprValue, elementType.CreateOrFetchType(unit));
            }
            else
                return unit.FetchIntegerType(exprValue);
        }

        public bool IsFunctionType => elementType.IsFunctionType;
    
        public string Dump()
        {
            return $"[{constantExpression.Dump()}] {elementType.Dump()}";
        }
        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}
