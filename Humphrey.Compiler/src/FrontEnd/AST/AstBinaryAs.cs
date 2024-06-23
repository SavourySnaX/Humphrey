using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstBinaryAs : IExpression
    {
        IExpression lhs;
        IType rhs;
        public AstBinaryAs(IExpression left, IType right)
        {
            lhs = left;
            rhs = right;
        }
    
        public string Dump()
        {
            return $"as {lhs.Dump()} {rhs.Dump()}";
        }

        public ICompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            var valueLeft = lhs.ProcessConstantExpression(unit);
            return valueLeft.Cast(rhs);
        }

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            var vlhs = lhs.ProcessExpression(unit, builder);
            if (vlhs is CompilationConstantIntegerKind)
                return ProcessConstantExpression(unit);
            if (vlhs is CompilationConstantFloatKind)
                return ProcessConstantExpression(unit);

            var valueLeft = vlhs as CompilationValue;
            var typeRight = rhs.CreateOrFetchType(unit).compilationType;

            if (valueLeft.Type.Same(typeRight))
                return valueLeft;
            
            while (true)
            {
                if (valueLeft.Type is CompilationStructureType scst)
                {
                    if (scst.Fields.Length == 1)
                    {
                        valueLeft = scst.LoadElement(unit, builder, valueLeft, scst.Fields[0]);
                        continue;
                    }
                }
                break;
            }

            if (valueLeft.Type is CompilationFloatType && typeRight is CompilationIntegerType integerType)
            {
                if (integerType.IsSigned)
                {
                    return builder.FloatToSigned(valueLeft, typeRight);
                }
                if (!integerType.IsSigned)
                {
                    return builder.FloatToUnsigned(valueLeft, typeRight);
                }
            }
            if (valueLeft.Type is CompilationIntegerType iType && typeRight is CompilationFloatType)
            {
                if (iType.IsSigned)
                {
                    return builder.SignedToFloat(valueLeft, typeRight);
                }
                else
                {
                    return builder.UnsignedToFloat(valueLeft, typeRight);
                }
            }

            return builder.Cast(valueLeft, typeRight);
        }

        public IType ResolveExpressionType(SemanticPass pass)
        {
            return rhs;
        }

        public void Semantic(SemanticPass pass)
        {
            lhs.Semantic(pass);
            rhs.Semantic(pass);
            rhs = rhs.ResolveBaseType(pass);
        }

        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}




