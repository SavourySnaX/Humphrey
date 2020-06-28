using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstBinaryPlus : IExpression
    {
        IExpression lhs;
        IExpression rhs;
        public AstBinaryPlus(IExpression left, IExpression right)
        {
            lhs = left;
            rhs = right;
        }
    
        public bool Compile(CompilationUnit unit)
        {
            return false;
        }

        public string Dump()
        {
            return $"+ {lhs.Dump()} {rhs.Dump()}";
        }

        public CompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            var valueLeft = lhs.ProcessExpression(unit, builder);
            var valueRight = rhs.ProcessExpression(unit, builder);

            var result = builder.BackendValue.BuildAdd(valueLeft.BackendValue, valueRight.BackendValue);
            
            return new CompilationValue(result);
        }
    }
}


