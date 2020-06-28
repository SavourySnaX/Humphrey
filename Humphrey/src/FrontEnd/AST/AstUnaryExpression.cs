using System;
using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public static class AstUnaryExpression 
    {
        public static IExpression FetchUnaryExpression(IOperator oper, IExpression expression)
        {
            switch (oper.Dump())
            {
                case "+":
                    return new AstUnaryPlus(expression);
                case "-":
                    return new AstUnaryMinus(expression);
                default:
                    throw new NotImplementedException($"Unimplemented unary operator : {oper.Dump()}");
            }
        }
    }
}


