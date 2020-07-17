using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstBinaryReference : IExpression
    {
        IExpression lhs;
        AstIdentifier rhs;
        public AstBinaryReference(IExpression left, AstIdentifier right)
        {
            lhs = left;
            rhs = right;
        }
    
        public string Dump()
        {
            return $". {lhs.Dump()} {rhs.Dump()}";
        }

        public CompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            throw new System.NotImplementedException($"ProcessConstantExpression for reference operator is not implemented");
        }

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            var rlhs = lhs.ProcessExpression(unit, builder);
            var vlhs = rlhs as CompilationValue;
            if (vlhs is null)
                throw new System.NotImplementedException($"Not sure it makes sense to have an a constant here");
            
            // we should now have a type on the left, and an identifier on the right
            var type = vlhs.Type as CompilationStructureType;
            if (type==null)
                throw new System.NotImplementedException($"Attempt to reference a structure member of a non structure type!");

            return type.LoadElement(unit, builder, vlhs, rhs.Dump());
        }
    }
}




