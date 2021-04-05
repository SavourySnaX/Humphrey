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

        public ICompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            throw new System.NotImplementedException($"ProcessConstantExpression for reference operator is not implemented");
        }

        public CompilationValue CommonProcessEnum(CompilationEnumType enumType, CompilationUnit unit, CompilationBuilder builder)
        {
            return enumType.LoadElement(unit, builder, rhs.Dump());
        }

        public CompilationValue CommonProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            var rlhs = lhs.ProcessExpression(unit, builder);

            if (rlhs == null)
            {
                throw new CompilationAbortException($"Aborting Compilation due to missing symbol");
            }

            var vlhs = rlhs as CompilationValue;
            if (vlhs is null)
                throw new System.NotImplementedException($"Not sure it makes sense to have an a constant here");

            var enumType = vlhs.Type as CompilationEnumType;
            if (enumType!=null)
                return CommonProcessEnum(enumType, unit, builder);

            // we should now have a struct type on the left, and an identifier on the right
            var pointerToValue = vlhs.Storage;
            var type = vlhs.Type as CompilationStructureType;
            if (type==null)
            {
                var pointerType = vlhs.Type as CompilationPointerType;
                if (pointerType != null && pointerType.ElementType is CompilationStructureType)
                {
                    type = pointerType.ElementType as CompilationStructureType;
                    pointerToValue = vlhs;
                }
                else
                {
                    // Compilation error... cannot fetch field from a non structure type 
                    throw new System.NotImplementedException($"Attempt to reference a structure member of a non structure type!");
                }
            }

            var store = type.AddressElement(unit, builder, pointerToValue, rhs.Dump());
            var loaded = new CompilationValue(builder.Load(store).BackendValue, (store.Type as CompilationPointerType).ElementType, Token);
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

        public IType ResolveExpressionType(SemanticPass pass)
        {
            var resolved = lhs.ResolveExpressionType(pass);
            var enumType = resolved as AstEnumType;
            if (enumType != null)
            {
                pass.AddEnumElementLocation(rhs.Token, enumType.Type);
                return enumType.Type;
            }

            var structType = resolved as AstStructureType;
            if (structType == null)
            {
                var pointerType = resolved as AstPointerType;
                if (pointerType!=null)
                {
                    structType = pointerType.ElementType as AstStructureType;
                }
            }

            if (structType != null)
            {
                foreach (var e in structType.Elements)
                {
                    foreach (var i in e.Identifiers)
                    {
                        if (rhs.Name == i.Name)
                        {
                            pass.AddStructElementLocation(rhs.Token, e.Type);
                            return e.Type;
                        }
                    }
                }
                throw new System.NotImplementedException($"error struct element not found");
            }
            
            throw new System.NotImplementedException($"TODO");
        }

        public void Semantic(SemanticPass pass)
        {
            lhs.Semantic(pass);
           // throw new System.Exception($"Should not reach here");
            /*
            var resolved = lhs.ResolveExpressionType(pass); // perhaps cache the types allways

            var enumType = resolved as AstEnumType;
            if (enumType != null)
            {
                // do enum

                return;
            }

            var structType = resolved as AstStructureType;
            if (structType == null)
            {
                var pointerType = resolved as AstPointerType;
                if (pointerType!=null)
                {
                    structType = pointerType.ElementType as AstStructureType;
                }
            }

            if (structType != null)
            {
                foreach (var e in structType.Elements)
                {
                    foreach (var i in e.Identifiers)
                    {
                        if (rhs.Name == i.Name)
                        {
                            pass.AddStructElementLocation(rhs.Token, e.Type);
                            return;
                        }
                    }
                }
                throw new System.NotImplementedException($"error struct element not found");
            }
            
            throw new System.NotImplementedException($"TODO");*/
        }

        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}




