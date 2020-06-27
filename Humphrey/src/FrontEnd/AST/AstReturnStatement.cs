using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public class AstReturnStatement : IStatement
    {
        IExpression expr;
        public AstReturnStatement(IExpression expression)
        {
            expr = expression;
        }

        public bool BuildStatement(CompilationUnit unit, CompilationFunction function, CompilationBuilder builder)
        {
            if (expr == null)
            {
                builder.BackendValue.BuildRetVoid();
            }
            else
            {
                var value = expr.ProcessExpression(unit, builder);

                builder.BackendValue.BuildRet(value.BackendValue);
            }

            return true;
        }

        public bool Compile(CompilationUnit unit)
        {
            return false;
        }
    
        public string Dump()
        {
            if (expr==null)
                return "return";
            return $"return {expr.Dump()}";
        }
    }
}

