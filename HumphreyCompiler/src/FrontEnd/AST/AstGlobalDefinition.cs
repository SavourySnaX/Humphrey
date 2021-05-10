using System.Text;
using Humphrey.Backend;
using static Extensions.Helpers;

namespace Humphrey.FrontEnd
{
    // May need splitting up into AstGlobalDefinition / AstLocalDefinition
    public class AstGlobalDefinition : IExpression, IGlobalDefinition
    {
        AstIdentifier[] identifiers;
        IType type;
        IAssignable initialiser;

        public AstGlobalDefinition(AstIdentifier[] identifierList, IType itype, IAssignable init)
        {
            identifiers = identifierList;
            type = itype;
            initialiser = init;
        }

        // Probably move this to compilation unit actually
        public static void CompileType(CompilationUnit unit, IType type, AstIdentifier identifier)
        {
            CompilationType ct = null;
            IType ot = default;

            if (type == null)
            {
                throw new System.Exception($"Should not occur, types should be resolved as part of semantic pass");
            }
            else 
            {
                // Potentially self referential structures need slightly different handling as we need to 
                //forward declare them prior to ensure if it is self referencing it can safely construct
                if (type is AstStructureType structureType)
                {
                    structureType.CreateOrFetchNamedStruct(unit, new AstIdentifier[] { identifier });
                    return;
                }
                else if (type is AstFunctionType astFunctionType && astFunctionType.IsGeneric)
                {
                    // We should not materialise this function, and instead should deal with it at the call site
                    return;
                }
                else
                {
                    (ct, ot) = type.CreateOrFetchType(unit);
                }
            }

            if (ct == null)
            {
                if (unit.Messages.HasErrors)
                    return;   // Attempt recovery from previous error
                throw new System.Exception($"Recovery attempt without prior error");
            }

            var functionType = ct as CompilationFunctionType;
            if (functionType != null)
            {
                if (functionType.FunctionCallingConvention == CompilationFunctionType.CallingConvention.CDecl)
                {
                    // Instead of creating a type, create an external function reference instead
                    unit.CreateExternalCFunction(functionType, identifier);
                    return;
                }
            }
            unit.CreateNamedType(identifier.Name, ct, ot);
        }


        public bool Compile(CompilationUnit unit)
        {
            // Resolve common things
            var codeBlock = initialiser as AstCodeBlock;
            var expr = initialiser as IExpression;
            var exprValue = expr?.ProcessConstantExpression(unit);
            CompilationType ct = null;
            IType ot = default;

            if (type == null)
            {
                throw new System.Exception($"Should not occur, types should be resolved as part of semantic pass");
            }
            else 
            {
                // Potentially self referential structures need slightly different handling as we need to 
                //forward declare them prior to ensure if it is self referencing it can safely construct
                if (initialiser == null && type is AstStructureType structureType)
                {
                    structureType.CreateOrFetchNamedStruct(unit, identifiers);
                    return false;
                }
                else if (codeBlock != null && type is AstFunctionType astFunctionType && astFunctionType.IsGeneric)
                {
                    // We should not materialise this function, and instead should deal with it at the call site
                    return false;
                }
                else
                {
                    (ct, ot) = type.CreateOrFetchType(unit);
                }
            }

            if (ct == null)
            {
                if (unit.Messages.HasErrors)
                    return false;   // Attempt recovery from previous error
                throw new System.Exception($"Recovery attempt without prior error");
            }

            foreach (var ident in identifiers)
            {
                var functionType = ct as CompilationFunctionType;
                if (functionType != null && initialiser == null)
                {
                    if (functionType.FunctionCallingConvention==CompilationFunctionType.CallingConvention.CDecl)
                    {
                        // Instead of creating a type, create an external function reference instead
                        unit.CreateExternalCFunction(functionType, ident);
                    }
                    else
                    {
                        unit.CreateNamedType(ident.Name, ct, ot);
                    }
                }
                else if (functionType != null && initialiser != null && codeBlock != null)
                {
                    var ft = ot as AstFunctionType;
                    ft.BuildFunction(unit, functionType, ident, codeBlock);
                }
                else if (initialiser == null)
                {
                    unit.CreateNamedType(ident.Name, ct, ot);
                }
                else
                {
                    var varName = ident.Name;
                    var location = new SourceLocation(Token);
                    var (newGlobal,adjustType) = unit.CreateGlobalVariable(ct, ident, location, exprValue);

                    // Debug information
                    var gve = unit.CreateGlobalVariableExpression(varName, location, adjustType.DebugType);
                    newGlobal.BackendValue.SetGlobalMetadata(LLVMSharp.Interop.LLVMMetadataKind.LLVMMDStringMetadataKind, gve);
                }
            }

            return false;
        }
    
        public string Dump()
        {
            var s = new StringBuilder();
            for (int a=0;a<identifiers.Length;a++)
            {
                if (a!=0)
                    s.Append(" , ");
                s.Append(identifiers[a].Dump());
            }
            if (type==null)
                s.Append($" := {initialiser.Dump()}");
            else if (initialiser==null)
                s.Append($" : {type.Dump()}");
            else
                s.Append($" : {type.Dump()} = {initialiser.Dump()}");

            return s.ToString();
        }

        public ICompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            throw new System.NotImplementedException($"Todo implement constant expression processing");
        }

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            throw new System.NotImplementedException();
        }

        public void Semantic(SemanticPass pass)
        {
            var codeBlock = initialiser as AstCodeBlock;
            var expr = initialiser as IExpression;
            IType ot = default;

            if (type == null)
            {
                if (expr != null)
                {
                    type = expr.ResolveExpressionType(pass);
                }

                if (type == null)
                {
                    pass.Messages.Log(CompilerErrorKind.Error_UndefinedType, "Cannot infer type from initialiser", Token.Location, Token.Remainder);
                    return;
                }

            }

            ot = type.ResolveBaseType(pass);
            foreach (var ident in identifiers)
            {
                var functionType = ot as AstFunctionType;
                if (functionType != null && initialiser == null)
                {
                    if (!pass.AddType(ident, functionType))
                    {
                        pass.Messages.Log(CompilerErrorKind.Error_DuplicateSymbol, $"A symbol called {ident.Name} already exists", ident.Token.Location, ident.Token.Remainder);
                    }
                    functionType.Semantic(pass, null);
                }
                else if (functionType != null && initialiser != null && codeBlock != null)
                {
                    if (!pass.AddFunction(ident, functionType))
                    {
                        pass.Messages.Log(CompilerErrorKind.Error_DuplicateSymbol, $"A symbol called {ident.Name} already exists", ident.Token.Location, ident.Token.Remainder);
                    }
                    functionType.Semantic(pass, codeBlock);

                    if (functionType.IsGeneric)
                    {
                        functionType.SetGenericInitialiser(codeBlock, ident.Name);
                    }
                }
                else if (initialiser == null)
                {
                    if (!pass.AddType(ident, type))
                    {
                        pass.Messages.Log(CompilerErrorKind.Error_DuplicateSymbol, $"A symbol called {ident.Name} already exists", ident.Token.Location, ident.Token.Remainder);
                    }
                    type.Semantic(pass);
                }
                else
                {
                    if (!pass.AddValue(ident, SemanticPass.IdentifierKind.GlobalValue, type))
                    {
                        pass.Messages.Log(CompilerErrorKind.Error_DuplicateSymbol, $"A symbol called {ident.Name} already exists", ident.Token.Location, ident.Token.Remainder);
                    }
                    type.Semantic(pass);
                    expr.Semantic(pass);
                }
            }
        }

        public IType ResolveExpressionType(SemanticPass pass)
        {
            return type;
        }

        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

        public AstIdentifier[] Identifiers => identifiers;

        public IAssignable Initialiser=> initialiser;
    }
}


