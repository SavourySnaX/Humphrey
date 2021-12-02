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
            valueLeft.Cast(rhs);
            return valueLeft;
        }

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            var vlhs = lhs.ProcessExpression(unit, builder);
            if (vlhs is CompilationConstantIntegerKind)
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




