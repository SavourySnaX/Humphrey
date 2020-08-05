using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstBinaryReference : IExpression, IStorable
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


        public CompilationValue CommonProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            var rlhs = lhs.ProcessExpression(unit, builder);
            var vlhs = rlhs as CompilationValue;
            if (vlhs is null)
                throw new System.NotImplementedException($"Not sure it makes sense to have an a constant here");
            
            // we should now have a type on the left, and an identifier on the right
            var type = vlhs.Type as CompilationStructureType;
            if (type==null)
                throw new System.NotImplementedException($"Attempt to reference a structure member of a non structure type!");

            var store = type.AddressElement(unit, builder, vlhs.Storage, rhs.Dump());
            var loaded = new CompilationValue(builder.Load(store).BackendValue, (store.Type as CompilationPointerType).ElementType);
            loaded.Storage = store;

            return loaded;
        }
        
        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            return CommonProcessExpression(unit, builder);
        }

        public void ProcessExpressionForStore(CompilationUnit unit, CompilationBuilder builder, IExpression value)
        {
            var dst = CommonProcessExpression(unit, builder);

            var storeValue = AstUnaryExpression.EnsureTypeOk(unit, builder, value, dst.Type);

            builder.Store(storeValue, dst.Storage);
        }
        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}




