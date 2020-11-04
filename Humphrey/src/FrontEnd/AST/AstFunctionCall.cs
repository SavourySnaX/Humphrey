using System.Text;
using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public class AstFunctionCall : IStatement,IExpression,ILoadValue
    {
        IExpression expr;
        AstExpressionList argumentList;
        public AstFunctionCall(IExpression expression, AstExpressionList arguments)
        {
            argumentList = arguments;
            expr = expression;
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

        public CompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            throw new System.NotImplementedException($"Todo implement constant expression for call....");
        }
        unsafe delegate void Testing(System.UInt32 a, System.UInt32 b, System.UInt32 c, System.UInt32* retVal);

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            // compute expr (should be a functionpointertype)
            var function = expr.ProcessExpression(unit, builder) as CompilationValue;
            if (function==null)
            {
                throw new System.NotImplementedException($"Todo - constant compilation value....");
            }
            var ftype = function.Type as CompilationFunctionType;
            if (ftype==null)
            {
                var ptrToFunction = function.Type as CompilationPointerType;
                if (ptrToFunction==null)
                    throw new System.NotImplementedException($"Todo - not a function type... pointer to function type?");
                ftype = ptrToFunction.ElementType as CompilationFunctionType;
                if (ftype==null)
                    throw new System.NotImplementedException($"Todo - not a function type... pointer to function type?");
                // need to swap the type to be functiontype and not pointer, but we don't want to dereference the value
                function = new CompilationValue(function.BackendValue, ftype, Token);
            }

            CompilationValue allocSpace = default;

            unsafe
            {
                if (ftype.CompileTimeOnly)
                {
                    var evaluate = unit.GetJITEvaluator();
                    if (evaluate.CompileMissing("VK_API_VERSION"))
                    {
                        var jittedMethod = evaluate.JitMethod("VK_API_VERSION");
                        var func = System.Runtime.InteropServices.Marshal.GetDelegateForFunctionPointer<Testing>(jittedMethod);
                        System.UInt32 returnValue;
                        func(1, 0, 0, &returnValue);
                        return unit.CreateConstant($"{returnValue}", new SourceLocation(Token));
                    }
                }
            }
            // create an anonymous struct to hold the outputs of the function..
            var structType = ftype.CreateOutputParameterStruct(unit, ftype.Location);
            if (structType != null) // not void function
            {
                allocSpace = builder.Alloca(structType);
                // we might want to always set this for alloca...
                allocSpace.Storage = new CompilationValue(allocSpace.BackendValue, unit.CreatePointerType(structType, new SourceLocation(argumentList.Token)), argumentList.Token);
            }
            // pass the input expression results to the input arguments  
            var arguments = new CompilationValue[ftype.Parameters.Length];
            if (argumentList.Expressions.Length != ftype.InputCount)
                throw new System.Exception($"TODO - this is an error, function call doesn't match function arguments");
            for (uint a = 0; a < argumentList.Expressions.Length;a++)
            {
                var exprResult = argumentList.Expressions[a].ProcessExpression(unit, builder);
                var value = Expression.ResolveExpressionToValue(unit, exprResult, ftype.Parameters[a].Type);
                arguments[a] = value;
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
            if (ftype.FunctionCallingConvention==CompilationFunctionType.CallingConvention.CDecl)
                return result;

            if (structType==null)
                return null;        // undef?

            // return the anonymous struct as the ICompilationValue
            return builder.Load(allocSpace);
        }
        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}


