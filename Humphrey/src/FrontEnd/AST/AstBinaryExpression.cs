using System;
using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public static class AstBinaryExpression
    {
        public static IExpression FetchBinaryExpression(IOperator oper, IExpression left, IExpression right)
        {
            switch (oper.Dump())
            {
                case "+":
                    return new AstBinaryPlus(left, right);
                case "-":
                    return new AstBinaryMinus(left, right);
                case "*":
                    return new AstBinaryMultiply(left, right);
                case "/":
                    return new AstBinaryDivide(left, right);
                case "%":
                    return new AstBinaryModulus(left, right);
                default:
                    throw new NotImplementedException($"Unimplemented binary operator : {oper.Dump()}");
            }
        }
    }
}

