using System;
using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public static class AstUnaryExpression 
    {
        public static IExpression FetchUnaryExpression(IOperator oper, IExpression expression)
        {
            IExpression unary = default;
            switch (oper.Dump())
            {
                case "+":
                    unary = new AstUnaryPlus(expression);
                    break;
                case "-":
                    unary = new AstUnaryMinus(expression);
                    break;
                case "*":
                    unary = new AstUnaryDereference(expression);
                    break;
                case "&":
                    unary = new AstUnaryAddressOf(expression);
                    break;
                case "!":
                    unary = new AstUnaryLogicalNot(expression);
                    break;
                case "~":
                    unary = new AstUnaryBinaryNot(expression);
                    break;
                case "++":
                    unary = new AstUnaryPreIncrement(expression);
                    break;
                case "--":
                    unary = new AstUnaryPreDecrement(expression);
                    break;
                default:
                    throw new NotImplementedException($"Unimplemented unary operator : {oper.Dump()}");
            }
            unary.Token = expression.Token;
            return unary;
        }
        public static CompilationValue EnsureTypeOk(CompilationUnit unit, CompilationBuilder builder, IExpression expr, CompilationType destType)
        {
            var result = expr.ProcessExpression(unit, builder);
            if (result == null)
            {
                if (unit.Messages.HasErrors)
                    return unit.CreateUndef(destType);  // Allow recovery from a missing value error
                throw new System.Exception($"Recovery attempt without prior error");
            }
            CompilationValue src = Expression.ResolveExpressionToValue(unit, result, destType);

            if (src.Type is CompilationFunctionType compilationFunctionType)
            {
                src = new CompilationValue(src.BackendValue, unit.CreatePointerType(compilationFunctionType, compilationFunctionType.Location), src.FrontendLocation);
            }

            while (true)
            {
                if (src.Type.Same(destType))
                    return src;

                // Special flexible case if a structure has a single element, we can directly unpack (this may need to be in a loop)
                if (src.Type is CompilationStructureType cst)
                {
                    if (cst.Fields.Length == 1)
                    {
                        src = cst.LoadElement(unit, builder, src, cst.Fields[0]);
                        continue;
                    }
                }
                break;
            }
            var srcEnum = src.Type as CompilationEnumType;
            var destEnum = destType as CompilationEnumType;

            // Always promote integer type to largest of two sizes if not matching is the current rule..
            var srcIntType = src.Type as CompilationIntegerType;
            var destIntType = destType as CompilationIntegerType;

            if (srcEnum != null)
            {
                srcIntType = srcEnum.ElementType as CompilationIntegerType;
                src = new CompilationValue(src.BackendValue, srcIntType, src.FrontendLocation);
            }
            if (destEnum != null)
                destIntType = destEnum.ElementType as CompilationIntegerType;

            if (srcIntType != null && destIntType != null)
            {
                if (srcIntType.IntegerWidth == destIntType.IntegerWidth)
                {
                    if (srcIntType.IsSigned == destIntType.IsSigned)
                    {
                        return src;
                    }

                    throw new NotImplementedException($"TODO - signed/unsigned mismatch");
                }
                else if (srcIntType.IntegerWidth < destIntType.IntegerWidth)
                {
                    // For integers, if the the size is strictly less, then the assignment is always allowed (we just upcast the value to the new bitwidth)
                    return builder.Ext(src, destType);
                }

                unit.Messages.Log(CompilerErrorKind.Error_IntegerWidthMismatch, $"Result of expression '{expr.Token.Location.ToStringValue(expr.Token.Remainder)}' of type '{srcIntType.DumpType()}' is larger than {destIntType.DumpType()}!", expr.Token.Location, expr.Token.Remainder);
                return unit.CreateUndef(destType);  // Allow compilation to continue
            }
            throw new NotImplementedException($"TODO - Non integer types in promotion?");
        }
    }
}


