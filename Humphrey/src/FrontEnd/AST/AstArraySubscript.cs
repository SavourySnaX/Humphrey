using System.Text;
using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public class AstArraySubscript : IStatement,IExpression,ILoadValue
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

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            var rlhs = expr.ProcessExpression(unit, builder);
            var rrhs = subscriptIdx.ProcessExpression(unit, builder);
            if (rlhs is CompilationConstantValue clhs && rrhs is CompilationConstantValue crhs)
                return ProcessConstantExpression(unit);

            var vlhs = rlhs as CompilationValue;
            var vrhs = rrhs as CompilationValue;

            if (vlhs is null)
                vlhs = (rlhs as CompilationConstantValue).GetCompilationValue(unit, vrhs.Type);
            if (vrhs is null)
                vrhs = (rrhs as CompilationConstantValue).GetCompilationValue(unit, unit.FetchIntegerType(64));

            if (vlhs.Type is CompilationPointerType pointerType)
            {
                var gep = builder.InBoundsGEP(vlhs, new LLVMSharp.Interop.LLVMValueRef[] { builder.Ext(vrhs, unit.FetchIntegerType(64)).BackendValue });
                var dereferenced = builder.Load(gep);
                return new CompilationValue(dereferenced.BackendValue, pointerType.ElementType);
            }

            throw new System.NotImplementedException($"Todo implement expression for subscript of array type...");
        }
    }
}



