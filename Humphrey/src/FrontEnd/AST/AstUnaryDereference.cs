using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public class AstUnaryDereference : IExpression
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

        public CompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            throw new System.Exception($"Cannot derefence a constant expression");
        }

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            var value = expr.ProcessExpression(unit, builder);
            if (value is CompilationConstantValue constantValue)
            {
                throw new System.Exception($"Cannot derefence a constant expression");
            }
            else
            {
                var compilationValue = value as CompilationValue;
                if (!compilationValue.Type.IsPointer)
                    throw new System.Exception($"Cannot derefence a non pointer type");
                var dereferenced = builder.Load(compilationValue);
                return new CompilationValue(dereferenced.BackendValue, dereferenced.Type.RemovePointer());
            }
        }
    }
}

