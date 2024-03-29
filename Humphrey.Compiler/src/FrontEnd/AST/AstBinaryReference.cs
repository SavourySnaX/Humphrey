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

        public (CompilationValue cv, CompilationAliasType alias) CommonProcessEnum(CompilationEnumType enumType, CompilationUnit unit, CompilationBuilder builder)
        {
            return (enumType.LoadElement(unit, builder, rhs.Name), null);
        }

        public (CompilationValue cv, CompilationAliasType alias) CommonProcessExpression(CompilationUnit unit, CompilationBuilder builder)
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
            }

            if (type != null)
            {
                var store = type.AddressElement(unit, builder, pointerToValue, rhs.Dump());
                var loaded = new CompilationValue(builder.Load(store).BackendValue, (store.Type as CompilationPointerType).ElementType, Token);
                loaded.Storage = store;

                return (loaded, null);
            }

            var aliasType = vlhs.Type as CompilationAliasType;
            if (aliasType == null)
            {
                var pointerType = vlhs.Type as CompilationPointerType;
                if (pointerType!=null && pointerType.ElementType is CompilationAliasType)
                {
                    aliasType = pointerType.ElementType as CompilationAliasType;
                    pointerToValue = vlhs;
                }
            }

            if (aliasType!=null)
            {
                // Load base value, mask element, shift down, returning as the type
                var store = pointerToValue;
                var loaded = new CompilationValue(builder.Load(store).BackendValue, (store.Type as CompilationPointerType).ElementType, Token);
                loaded.Storage = store;
                return (loaded,aliasType);
            }

            // Compilation error... cannot fetch field from a non field based type 
            unit.Messages.Log(CompilerErrorKind.Error_ExpectedType, $"Attempted to get element from a '{vlhs.Type.DumpType()}' this is not possible", Token.Location, Token.Remainder);
            return (null, null);
        }
        
        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            var res = CommonProcessExpression(unit, builder);
            if (res.alias!=null)
            {
                return res.alias.LoadElement(unit,builder, res.cv, rhs.Name);
            }
            return res.cv;
        }

        public void ProcessExpressionForStore(CompilationUnit unit, CompilationBuilder builder, IExpression value)
        {
            var dst = CommonProcessExpression(unit, builder);
            if (dst.alias!=null)
            {
                dst.alias.StoreElement(unit, builder, dst.cv, value, rhs.Name );
                return;
            }

            var storeValue = AstUnaryExpression.EnsureTypeOk(unit, builder, value, dst.cv.Type);

            builder.Store(storeValue, dst.cv.Storage);
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

            var aliasType = baseResolved as AstAliasType;
            if (aliasType == null)
            {
                var pointerType = baseResolved as AstPointerType;
                if (pointerType!=null)
                {
                    aliasType = pointerType.ElementType.ResolveBaseType(pass) as AstAliasType;
                }
            }

            if (aliasType != null)
            {
                if (rhs.Name=="raw")
                {
                    return aliasType.RawType;
                }

                foreach (var e in aliasType.Elements)
                {
                    foreach (var i in e.Identifiers)
                    {
                        if (rhs.Name == i.Name)
                        {
                            return e.Type;
                        }
                    }
                }
                pass.Messages.Log(CompilerErrorKind.Error_StructMemberDoesNotExist, $"LHS alias does not contain a member '{rhs.Name}'", rhs.Token.Location, rhs.Token.Remainder);
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

            var aliasType = baseResolved as AstAliasType;
            if (aliasType == null)
            {
                var pointerType = baseResolved as AstPointerType;
                if (pointerType!=null)
                {
                    aliasType = pointerType.ElementType.ResolveBaseType(pass) as AstAliasType;
                }
            }

            if (aliasType!= null)
            {
                if (rhs.Name=="raw")
                {
                    pass.AddStructElementLocation(rhs.Token, aliasType.RawType);
                    return;
                }

                foreach (var e in aliasType.Elements)
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
                pass.Messages.Log(CompilerErrorKind.Error_StructMemberDoesNotExist, $"LHS alias does not contain a member '{rhs.Name}'", rhs.Token.Location, rhs.Token.Remainder);
                return;
            }

            pass.Messages.Log(CompilerErrorKind.Error_UndefinedType, $"Cannot determine result type from expression", Token.Location, Token.Remainder);
            return;
        }

        public IExpression LHS => lhs;
        public AstIdentifier RHS => rhs;

        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}




