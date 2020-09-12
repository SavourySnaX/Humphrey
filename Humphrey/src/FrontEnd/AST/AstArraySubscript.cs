using static Extensions.Helpers;
using Humphrey.Backend;
using static Humphrey.Backend.CompilationBuilder;

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
                vrhs = (rrhs as CompilationConstantValue).GetCompilationValue(unit, unit.FetchIntegerType(64, false, new SourceLocation(subscriptIdx.Token)));

            return (vlhs, vrhs);
        }

        public (CompilationValue left, CompilationValue rangeBegin, CompilationValue rangeEnd) CommonExpressionProcessForRange(CompilationUnit unit, CompilationBuilder builder)
        {
            var rlhs = expr.ProcessExpression(unit, builder);
            var range = subscriptIdx as AstInclusiveRange;
            var brhs = range.InclusiveStart?.ProcessExpression(unit, builder);
            var erhs = range.InclusiveEnd?.ProcessExpression(unit,builder);
            if (rlhs is CompilationConstantValue clhs && brhs is CompilationConstantValue cbrhs && erhs is CompilationConstantValue cerhs)
                throw new System.NotImplementedException($"Array subscript on constant is not possible yet?");

            var vlhs = rlhs as CompilationValue;
            CompilationValue vbrhs = null;
            CompilationValue verhs = null;

            if (vlhs is null)
                vlhs = (rlhs as CompilationConstantValue).GetCompilationValue(unit, null);

            if (brhs != null)
            {
                vbrhs = brhs as CompilationValue;
                if (vbrhs is null)
                    vbrhs = (brhs as CompilationConstantValue).GetCompilationValue(unit, unit.FetchIntegerType(64, false, new SourceLocation(range.InclusiveStart.Token)));
            }
            if (erhs != null)
            {
                verhs = erhs as CompilationValue;
                if (verhs is null)
                    verhs = (erhs as CompilationConstantValue).GetCompilationValue(unit, unit.FetchIntegerType(64, false, new SourceLocation(range.InclusiveEnd.Token)));
            }
            return (vlhs, vbrhs, verhs);
        }

        public ICompilationValue ProcessSingleSubscript(CompilationUnit unit, CompilationBuilder builder)
        {
            var i64Type = unit.FetchIntegerType(64, false, new SourceLocation());
            var (vlhs, vrhs) = CommonExpressionProcess(unit, builder);

            if (vlhs.Type is CompilationPointerType pointerType)
            {
                var gep = builder.InBoundsGEP(vlhs, pointerType, new LLVMSharp.Interop.LLVMValueRef[] { builder.Ext(vrhs, i64Type).BackendValue });
                var dereferenced = builder.Load(gep);
                return new CompilationValue(dereferenced.BackendValue, pointerType.ElementType);
            }
            if (vlhs.Type is CompilationArrayType arrayType)
            {
                var gep = builder.InBoundsGEP(vlhs.Storage, vlhs.Storage.Type as CompilationPointerType, new LLVMSharp.Interop.LLVMValueRef[] { i64Type.BackendType.CreateConstantValue(0), builder.Ext(vrhs, unit.FetchIntegerType(64, false, new SourceLocation(subscriptIdx.Token))).BackendValue });
                var dereferenced = builder.Load(gep);
                return new CompilationValue(dereferenced.BackendValue, arrayType.ElementType);
            }
            if (vlhs.Type is CompilationIntegerType integerType)
            {
                var bitType = unit.FetchIntegerType(1, false, new SourceLocation(subscriptIdx.Token));
                var matchWidth = builder.MatchWidth(vrhs, integerType);
                var rotated = builder.RotateRight(vlhs, matchWidth);
                return builder.Trunc(rotated, bitType);
            }

            throw new System.NotImplementedException($"Todo implement expression for subscript of array type...");
        }

        public ICompilationValue ProcessRangeSubscript(CompilationUnit unit, CompilationBuilder builder)
        {
            var i64Type = unit.FetchIntegerType(64, false, new SourceLocation());
            var (vlhs, rangeBegin, rangeEnd) = CommonExpressionProcessForRange(unit, builder);

            if (vlhs.Type is CompilationPointerType pointerType)
            {
                throw new System.NotImplementedException($"TODO pointer extraction");
                /*var gep = builder.InBoundsGEP(vlhs, pointerType, new LLVMSharp.Interop.LLVMValueRef[] { builder.Ext(vrhs, i64Type).BackendValue });
                var dereferenced = builder.Load(gep);
                return new CompilationValue(dereferenced.BackendValue, pointerType.ElementType);*/
            }
            if (vlhs.Type is CompilationArrayType arrayType)
            {
                throw new System.NotImplementedException($"TODO array extraction");
                /*var gep = builder.InBoundsGEP(vlhs.Storage, vlhs.Storage.Type as CompilationPointerType, new LLVMSharp.Interop.LLVMValueRef[] { i64Type.BackendType.CreateConstantValue(0), builder.Ext(vrhs, unit.FetchIntegerType(64)).BackendValue });
                var dereferenced = builder.Load(gep);
                return new CompilationValue(dereferenced.BackendValue, arrayType.ElementType);*/
            }
            if (vlhs.Type is CompilationIntegerType integerType)
            {
                var const0 = new CompilationValue(integerType.BackendType.CreateConstantValue(0), integerType);
                var const1 = new CompilationValue(integerType.BackendType.CreateConstantValue(1), integerType);
                CompilationValue rangeBeginMatchedBeforeCheck;
                CompilationValue rangeEndMatchedBeforeCheck;
                if (rangeBegin == null)
                {
                    rangeBeginMatchedBeforeCheck = builder.MatchWidth(const0, integerType);
                }
                else
                {
                    rangeBeginMatchedBeforeCheck = builder.MatchWidth(rangeBegin, integerType);
                }
                if (rangeEnd == null)
                {
                    var t = new CompilationValue(integerType.BackendType.CreateConstantValue(integerType.IntegerWidth), integerType);
                    var m = builder.MatchWidth(t, integerType);
                    rangeEndMatchedBeforeCheck = builder.Add(rangeBeginMatchedBeforeCheck, m);
                }
                else
                {
                    rangeEndMatchedBeforeCheck = builder.MatchWidth(rangeEnd, integerType);
                }

                var swapRangeCondition = builder.Compare(CompareKind.SLT, rangeEndMatchedBeforeCheck, rangeBeginMatchedBeforeCheck);
                var rangeEndMatched = builder.Select(swapRangeCondition, rangeBeginMatchedBeforeCheck, rangeEndMatchedBeforeCheck);
                var rangeBeginMatched = builder.Select(swapRangeCondition, rangeEndMatchedBeforeCheck, rangeBeginMatchedBeforeCheck);

                var shifted = builder.RotateRight(vlhs, rangeBeginMatched);
                var numBitsM1 = builder.Sub(rangeEndMatched, rangeBeginMatched);
                var numBits = builder.Add(numBitsM1, const1);
                var constM1 = builder.Not(const0);
                var maskMarker = builder.ShiftLeft(const1, numBits);
                var mask = builder.Sub(maskMarker, const1);
                var compareWidth = new CompilationValue(integerType.BackendType.CreateConstantValue(integerType.IntegerWidth), integerType);
                var cond = builder.Compare(CompareKind.ULT, numBits, compareWidth);
                var realMask = builder.Select(cond, mask, constM1);
                return builder.And(shifted, realMask);
            }

            throw new System.NotImplementedException($"Todo implement expression for subscript of array type...");
        }

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            if (subscriptIdx is AstInclusiveRange)
                return ProcessRangeSubscript(unit, builder);

            return ProcessSingleSubscript(unit, builder);
        }

        public void ProcessExpressionForStore(CompilationUnit unit, CompilationBuilder builder,IExpression value)
        {
            var i64Type = unit.FetchIntegerType(64, false, new SourceLocation());
            var (vlhs, vrhs) = CommonExpressionProcess(unit, builder);
            if (vlhs.Type is CompilationPointerType pointerType)
            {
                CompilationType elementType = pointerType.ElementType;
                var gep = builder.InBoundsGEP(vlhs, pointerType, new LLVMSharp.Interop.LLVMValueRef[] { builder.Ext(vrhs, i64Type).BackendValue });
                var storeValue = AstUnaryExpression.EnsureTypeOk(unit, builder, value, elementType);
                builder.Store(storeValue, gep);
                return;
            }
            if (vlhs.Type is CompilationArrayType arrayType)
            {
                CompilationType elementType = arrayType.ElementType;
                var gep = builder.InBoundsGEP(vlhs.Storage, vlhs.Storage.Type as CompilationPointerType, new LLVMSharp.Interop.LLVMValueRef[] { i64Type.BackendType.CreateConstantValue(0), builder.Ext(vrhs, i64Type).BackendValue });
                var storeValue = AstUnaryExpression.EnsureTypeOk(unit, builder, value, elementType);
                builder.Store(storeValue, gep);
                return;
            }
            if (vlhs.Type is CompilationIntegerType integerType)
            {
                var mask = new CompilationValue(integerType.BackendType.CreateConstantValue(1), integerType);
                var matchWidth = builder.MatchWidth(vrhs, integerType);
                var rotatedMask = builder.RotateLeft(mask, matchWidth);
                var invertedMask = builder.Not(rotatedMask);
                var storeValue = AstUnaryExpression.EnsureTypeOk(unit, builder, value, integerType);
                var rotatedStore = builder.RotateLeft(storeValue, matchWidth);
                var masked = builder.And(vlhs, invertedMask);
                var inserted = builder.Or(masked, rotatedStore);
                builder.Store(inserted, vlhs.Storage);
                return;
            }


            throw new System.NotImplementedException($"Todo implement expression for store for subscript of array type...");
        }
        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}



