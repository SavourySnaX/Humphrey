using System;
using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstBinaryMultiply : IExpression
    {
        IExpression lhs;
        IExpression rhs;
        public AstBinaryMultiply(IExpression left, IExpression right)
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
            return $"* {lhs.Dump()} {rhs.Dump()}";
        }

        public CompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            var valueLeft = lhs.ProcessExpression(unit, builder);
            var valueRight = rhs.ProcessExpression(unit, builder);

            var result = builder.BackendValue.BuildMul(valueLeft.BackendValue, valueRight.BackendValue ,"".AsSpan());       // Workaround stack overflow in LLVMSharp 10.0

            return new CompilationValue(result);
        }
    }
}



