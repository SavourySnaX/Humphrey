using System.Text;
using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public class AstFunctionCall : IStatement,IExpression,ILoadValue
    {
        IExpression expr;
        AstExpressionList argumentList;
        AstFunctionType functionType;   // filled by semantic pass
        IType[] resolvedInputs;         // filled by semantic pass
        public AstFunctionCall(IExpression expression, AstExpressionList arguments)
        {
            argumentList = arguments;
            expr = expression;
            functionType = null;
            resolvedInputs = null;
        }

        public bool BuildStatement(CompilationUnit unit, CompilationFunction function, CompilationBuilder builder)
        {
            throw new System.NotImplementedException($"FunctionCallStatement TODO");
        }

        public string Dump()
        {
            if (argumentList.Expressions.Length==0)
                return $"{expr.Dump()} ( )";
            return $"{expr.Dump()} ( {argumentList.Dump()} )";
        }

        public ICompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            throw new System.NotImplementedException($"Todo implement constant expression for call....");
        }

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            if (functionType==null)
            {
                throw new System.InvalidOperationException($"Should have been initialised by semantic pass");
            }


            if (functionType.IsGeneric)
            {
                // We need to now materialse the real function -- for now, just create a new function each use (if types differ)...
                // we need the parameter kinds to be computed so we know how to materialise the function
                // another todo - we need to build the function at the original scope, but at present we don't have that scope!

                var namespaced = expr as AstNamespaceIdentifier;

                var recoverTo = namespaced?.PushNamespace(unit);
                (var ct, var ot) = functionType.CreateOrFetchType(unit, resolvedInputs);
                namespaced?.PopNamespace(unit, recoverTo.Value);

                var ft = ot as AstFunctionType;

                var inputs = ComputeInputValues(unit, builder, ct);

                var genericName = ft.GenericBaseName;
                foreach (var i in inputs)
                {
                    genericName += i.Type.DumpType();
                }

                var name = new AstIdentifier(genericName);
                name.Token = Token;

                var function = unit.FetchValueIfDefined(name, builder);
                if (function == null)
                {
                    ft.BuildFunction(unit, ct, name);
                    function = unit.FetchValue(name, builder);
                }

                var ftype = ct;

                return CallMethod(unit, builder, function, ftype, inputs);
            }
            else
            {

                // compute expr (should be a functionpointertype)
                var function = expr.ProcessExpression(unit, builder) as CompilationValue;
                if (function == null)
                {
                    throw new System.NotImplementedException($"Todo - constant compilation value....");
                }
                var ftype = function.Type as CompilationFunctionType;
                if (ftype == null)
                {
                    var ptrToFunction = function.Type as CompilationPointerType;
                    if (ptrToFunction == null)
                        throw new System.NotImplementedException($"Todo - not a function type... pointer to function type?");
                    ftype = ptrToFunction.ElementType as CompilationFunctionType;
                    if (ftype == null)
                        throw new System.NotImplementedException($"Todo - not a function type... pointer to function type?");
                    // need to swap the type to be functiontype and not pointer, but we don't want to dereference the value
                    function = new CompilationValue(function.BackendValue, ftype, Token);
                }
                var inputValues = ComputeInputValues(unit, builder, ftype);

                return CallMethod(unit, builder, function, ftype, inputValues);
            }
        }

        private ICompilationValue CallMethod(CompilationUnit unit, CompilationBuilder builder, CompilationValue function, CompilationFunctionType ftype, CompilationValue[] inputs)
        {
            CompilationValue allocSpace = default;
            // create an anonymous struct to hold the outputs of the function..
            var structType = ftype.CreateOutputParameterStruct(unit, ftype.Location);
            if (structType != null) // not void function
            {
                allocSpace = builder.LocalBuilder.Alloca(structType);
                // we might want to always set this for alloca...
                allocSpace.Storage = new CompilationValue(allocSpace.BackendValue, unit.CreatePointerType(structType, new SourceLocation(argumentList.Token)), argumentList.Token);
            }
            var arguments = new CompilationValue[ftype.Parameters.Length];
            for (uint a = 0; a < inputs.Length;a++)
            {
                arguments[a] = AstUnaryExpression.EnsureTypeOk(unit, builder, inputs[a], ftype.Parameters[a].Type, argumentList.Expressions[a].Token);//ftype.Parameters[a].Token);
            }
            // and the anonymous struct positions to the outputs
            uint outArgIdx = ftype.OutParamOffset;
            for (uint a = ftype.OutParamOffset; a < ftype.Parameters.Length; a++)
            {
                var value = structType.AddressElement(unit, builder, allocSpace.Storage, ftype.Parameters[a].Identifier.Dump());
                arguments[a] = value;
            }
            // call the function
            var result = builder.Call(function, arguments);
            if (ftype.FunctionCallingConvention == CompilationFunctionType.CallingConvention.CDecl)
                return result;

            if (structType == null)
                return null;        // undef?

            // return the anonymous struct as the ICompilationValue
            return builder.Load(allocSpace);
        }

        private CompilationValue[] ComputeInputValues(CompilationUnit unit, CompilationBuilder builder, CompilationFunctionType ftype)
        {
            // pass the input expression results to the input arguments  
            if (argumentList.Expressions.Length != ftype.InputCount)
                throw new System.Exception($"TODO - this is an error, function call doesn't match function arguments");
            var arguments = new CompilationValue[ftype.InputCount];
            for (uint a = 0; a < argumentList.Expressions.Length; a++)
            {
                var exprResult = argumentList.Expressions[a].ProcessExpression(unit, builder);
                var value = Expression.ResolveExpressionToValue(unit, exprResult, ftype.Parameters[a].Type);
                arguments[a] = value;
            }

            return arguments;
        }

        public IType ResolveExpressionType(SemanticPass pass)
        {
            resolvedInputs = new IType[argumentList.Expressions.Length];
            for (int a = 0; a < resolvedInputs.Length;a++)
            {
                resolvedInputs[a] = argumentList.Expressions[a].ResolveExpressionType(pass);
            }
            var resolved = expr.ResolveExpressionType(pass);
            functionType = resolved as AstFunctionType;
            if (functionType == null)
            {
                var baseT = resolved.ResolveBaseType(pass);
                functionType = baseT as AstFunctionType;
                if (functionType == null)
                {
                    pass.Messages.Log(CompilerErrorKind.Error_UndefinedFunction, $"Cannot determine result type from function call", Token.Location, Token.Remainder);
                    return new AstBitType();
                }
            }
            return functionType.ResolveOutputType(pass);
        }

        public void Semantic(SemanticPass pass)
        {
            ResolveExpressionType(pass);    // we need this to handle void input functions
            expr.Semantic(pass);
            foreach (var e in argumentList.Expressions)
            {
                e.Semantic(pass);
            }
            pass.FetchSemanticInfo(expr.Token, out var info);
            pass.AddSemanticInfoToToken(info, argumentList.TokenForParenthesis);
        }

        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}


