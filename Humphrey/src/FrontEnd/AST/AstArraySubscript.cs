using static Extensions.Helpers;
using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public class AstArraySubscript : IStatement,IExpression,ILoadValue,IStorable
    {
        IExpression expr;
        IExpression subscriptIdx;
        public AstArraySubscript(IExpression expression, IExpression subscript)
        {
            subscriptIdx = subscript;
            expr = expression;
        }

        public bool BuildStatement(CompilationUnit unit, CompilationFunction function, CompilationBuilder builder)
        {
            throw new System.NotImplementedException($"ArraySubscript TODO");
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
            return $"{expr.Dump()} [ {subscriptIdx.Dump()} ]";
        }

        public CompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            throw new System.NotImplementedException($"Todo implement constant expression for subscript....");
        }

        public (CompilationValue left, CompilationValue right) CommonExpressionProcess(CompilationUnit unit, CompilationBuilder builder)
        {
            var rlhs = expr.ProcessExpression(unit, builder);
            var rrhs = subscriptIdx.ProcessExpression(unit, builder);
            if (rlhs is CompilationConstantValue clhs && rrhs is CompilationConstantValue crhs)
                throw new System.NotImplementedException($"Array subscript on constant is not possible yet?");

            var vlhs = rlhs as CompilationValue;
            var vrhs = rrhs as CompilationValue;

            if (vlhs is null)
                vlhs = (rlhs as CompilationConstantValue).GetCompilationValue(unit, vrhs.Type);
            if (vrhs is null)
                vrhs = (rrhs as CompilationConstantValue).GetCompilationValue(unit, unit.FetchIntegerType(64));

            return (vlhs, vrhs);
        }

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            var i64Type = unit.FetchIntegerType(64);
            var (vlhs, vrhs) = CommonExpressionProcess(unit, builder);

            if (vlhs.Type is CompilationPointerType pointerType)
            {
                var gep = builder.InBoundsGEP(vlhs, new LLVMSharp.Interop.LLVMValueRef[] { builder.Ext(vrhs, i64Type).BackendValue });
                var dereferenced = builder.Load(gep);
                return new CompilationValue(dereferenced.BackendValue, pointerType.ElementType);
            }
            if (vlhs.Type is CompilationArrayType arrayType)
            {
                var gep = builder.InBoundsGEP(vlhs.Storage, new LLVMSharp.Interop.LLVMValueRef[] { i64Type.BackendType.CreateConstantValue(0), builder.Ext(vrhs, unit.FetchIntegerType(64)).BackendValue });
                var dereferenced = builder.Load(gep);
                return new CompilationValue(dereferenced.BackendValue, arrayType.ElementType);
            }

            throw new System.NotImplementedException($"Todo implement expression for subscript of array type...");
        }

        public void ProcessExpressionForStore(CompilationUnit unit, CompilationBuilder builder,IExpression value)
        {
            var i64Type = unit.FetchIntegerType(64);
            var (vlhs, vrhs) = CommonExpressionProcess(unit, builder);
            if (vlhs.Type is CompilationPointerType pointerType)
            {
                var gep = builder.InBoundsGEP(vlhs, new LLVMSharp.Interop.LLVMValueRef[] { builder.Ext(vrhs, i64Type).BackendValue });
                CompilationType elementType = pointerType.ElementType;
                var storeValue = AstUnaryExpression.EnsureTypeOk(unit, builder, value, elementType);
                builder.Store(storeValue, gep);
                return;
            }
            if (vlhs.Type is CompilationArrayType arrayType)
            {
                var gep = builder.InBoundsGEP(vlhs.Storage, new LLVMSharp.Interop.LLVMValueRef[] { i64Type.BackendType.CreateConstantValue(0), builder.Ext(vrhs, i64Type).BackendValue });
                CompilationType elementType = arrayType.ElementType;
                var storeValue = AstUnaryExpression.EnsureTypeOk(unit, builder, value, elementType);
                builder.Store(storeValue, gep);
                return;
            }

            throw new System.NotImplementedException($"Todo implement expression for store for subscript of array type...");
        }
        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}



