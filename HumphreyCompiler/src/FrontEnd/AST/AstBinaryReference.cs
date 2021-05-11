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
            var baseResolved = resolved.ResolveBaseType(pass);
            var enumType = baseResolved as AstEnumType;
            if (enumType != null)
            {
                return enumType.Type;
            }

            var structType = baseResolved as AstStructureType;
            if (structType == null)
            {
                var pointerType = baseResolved as AstPointerType;
                if (pointerType!=null)
                {
                    structType = pointerType.ElementType.ResolveBaseType(pass) as AstStructureType;
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
                            return e.Type;
                        }
                    }
                }
                pass.Messages.Log(CompilerErrorKind.Error_StructMemberDoesNotExist, $"LHS structure does not contain a member '{rhs.Name}'", rhs.Token.Location, rhs.Token.Remainder);
            }

            pass.Messages.Log(CompilerErrorKind.Error_UndefinedType, $"Cannot determine result type from expression", Token.Location, Token.Remainder);
            return new AstBitType();
        }

        public void Semantic(SemanticPass pass)
        {
            lhs.Semantic(pass);
            var resolved = lhs.ResolveExpressionType(pass);
            var baseResolved = resolved.ResolveBaseType(pass);
            var enumType = baseResolved as AstEnumType;
            if (enumType != null)
            {
                pass.AddEnumElementLocation(rhs.Token, enumType.Type);
                return;
            }

            var structType = baseResolved as AstStructureType;
            if (structType == null)
            {
                var pointerType = baseResolved as AstPointerType;
                if (pointerType!=null)
                {
                    structType = pointerType.ElementType.ResolveBaseType(pass) as AstStructureType;
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
                pass.Messages.Log(CompilerErrorKind.Error_StructMemberDoesNotExist, $"LHS structure does not contain a member '{rhs.Name}'", rhs.Token.Location, rhs.Token.Remainder);
                return;
            }
        }

        public IExpression LHS => lhs;
        public AstIdentifier RHS => rhs;

        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}




