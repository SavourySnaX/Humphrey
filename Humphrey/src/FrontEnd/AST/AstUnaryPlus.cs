using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public class AstUnaryPlus : IExpression
    {
        IExpression expr;
        public AstUnaryPlus(IExpression expression)
        {
            expr = expression;
        }
    
        public bool Compile(CompilationUnit unit)
        {
            return false;
        }
    
        public string Dump()
        {
            return $"+ {expr.Dump()}";
        }

        public CompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            // + is 0 + which is noop
            return expr.ProcessConstantExpression(unit);
        }

        public CompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            var value = expr.ProcessExpression(unit, builder);

            var result = builder.BackendValue.BuildAdd(unit.CreateConstant("0").BackendValue, value.BackendValue);
            
            return new CompilationValue(result);
        }
    }
}

