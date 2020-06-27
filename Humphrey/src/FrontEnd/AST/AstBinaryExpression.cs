using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstBinaryExpression : IExpression
    {
        IOperator op;
        IExpression lhs;
        IExpression rhs;
        public AstBinaryExpression(IOperator oper, IExpression left, IExpression right)
        {
            op = oper;
            lhs = left;
            rhs = right;
        }
    
        public bool Compile(CompilationUnit unit)
        {
            return false;
        }

        public string Dump()
        {
            return $"{op.Dump()} {lhs.Dump()} {rhs.Dump()}";
        }

        public CompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            throw new System.NotImplementedException();
        }
    }
}

