using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstLoadableIdentifier : IExpression,IType,ILoadValue,IStorable
    {
        string temp;
        public AstLoadableIdentifier(string value)
        {
            temp = value;
        }
    
        public CompilationType CreateOrFetchType(CompilationUnit unit)
        {
            return unit.FetchNamedType(temp);
        }
    
        public bool IsFunctionType => false;
    
        public string Dump()
        {
            return temp;
        }
        public CompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            throw new System.NotImplementedException($"Todo implement constant expression processing for constant values");
        }

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            return unit.FetchValue(temp, builder);
        }

        public void ProcessExpressionForStore(CompilationUnit unit, CompilationBuilder builder, IExpression value)
        {
            var storeTo = unit.FetchLocation(temp, builder);
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
        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}

