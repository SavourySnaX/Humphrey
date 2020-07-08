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
        // Todo find better place for this
        public static CompilationValue EnsureTypeOk(CompilationUnit unit, CompilationValue left, CompilationType destType)
        {
            
            // Always promote integer type to largest of two sizes if not matching is the current rule..
            if (left.Type.IsIntegerType == true && destType.IsIntegerType == true)
            {
                if (left.Type.IntegerWidth == destType.IntegerWidth)
                {
                    if (left.Type.IsSigned == destType.IsSigned)
                    {
                        return left;
                    }

                    throw new NotImplementedException($"TODO - signed/unsigned mismatch");
                }

                throw new NotImplementedException($"TODO - Integer Bit width does not match");
            }

            throw new NotImplementedException($"TODO - Non integer types in promotion?");
        }
    }
}


