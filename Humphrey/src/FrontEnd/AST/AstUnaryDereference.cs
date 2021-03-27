using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public class AstUnaryDereference : IExpression, IStorable
    {
        IExpression expr;
        public AstUnaryDereference(IExpression expression)
        {
            expr = expression;
        }
    
        public string Dump()
        {
            return $"* {expr.Dump()}";
        }

        public ICompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            throw new System.Exception($"Cannot derefence a constant expression");
        }

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            var value = expr.ProcessExpression(unit, builder);
            if (value is CompilationConstantIntegerKind constantValue)
            {
                throw new System.Exception($"Cannot derefence a constant expression");
            }
            else
            {
                var compilationValue = value as CompilationValue;
                if (compilationValue==null)
                    throw new CompilationAbortException($"Cannot derefence an undefined value");
                var compilationPointerType = compilationValue.Type as CompilationPointerType;
                if (compilationPointerType == null)
                    throw new System.Exception($"Cannot derefence a non pointer type");
                var dereferenced = builder.Load(compilationValue);
                return new CompilationValue(dereferenced.BackendValue, compilationPointerType.ElementType, Token);
            }
        }

        public void ProcessExpressionForStore(CompilationUnit unit, CompilationBuilder builder, IExpression value)
        {
            var dest = expr.ProcessExpression(unit, builder);
            if (value is CompilationConstantIntegerKind constantValue)
            {
                throw new System.Exception($"Cannot derefence a constant expression");
            }
            else
            {
                var compilationValue = dest as CompilationValue;
                var compilationPointerType = compilationValue.Type as CompilationPointerType;
                if (compilationPointerType == null)
                    throw new System.Exception($"Cannot derefence a non pointer type");

                CompilationType elementType = compilationPointerType.ElementType;
                var storeValue = AstUnaryExpression.EnsureTypeOk(unit, builder, value, elementType);
                builder.Store(storeValue, compilationValue);
            }
        }

        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}

