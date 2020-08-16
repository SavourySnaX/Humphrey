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
            /*uint outParamIdx = function.OutParamOffset;
            foreach (var expr in exprList)
            {
                var paramType = function.FunctionType.Parameters[outParamIdx].Type;
                var value = AstUnaryExpression.EnsureTypeOk(unit, builder, expr, paramType);

                var parameter = function.BackendValue.GetParam(outParamIdx++);

                builder.BackendValue.BuildStore(value.BackendValue, parameter);
            }

            builder.BackendValue.BuildRetVoid();

            return true;*/
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

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            // Thinking how this will work :

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
            }

            CompilationValue allocSpace = default;

            // create an anonymous struct to hold the outputs of the function..
            var structType = ftype.CreateOutputParameterStruct(unit);
            if (structType != null) // not void function
            {
                allocSpace = builder.Alloca(structType);
                // we might want to always set this for alloca...
                allocSpace.Storage = new CompilationValue(allocSpace.BackendValue, new CompilationPointerType(Extensions.Helpers.CreatePointerType(structType.BackendType), structType));
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
                var value = structType.AddressElement(unit, builder, allocSpace.Storage, ftype.Parameters[a].Identifier);
                arguments[a] = value;
            }
            // call the function
            builder.Call(function, arguments);

            if (structType==null)
                return null;        // undef?

            // return the anonymous struct as the ICompilationValue
            return builder.Load(allocSpace);
        }
        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}


