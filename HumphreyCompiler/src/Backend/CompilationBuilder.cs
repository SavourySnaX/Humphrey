using System;
using System.Collections.Generic;
using Humphrey.FrontEnd;
using LLVMSharp.Interop;

namespace Humphrey.Backend
{
    public class CompilationBuilder
    {
        CompilationUnit unit;
        LLVMBuilderRef builderRef;
        CompilationFunction function;
        CompilationBlock currentBlock;

        CompilationBuilder localsBuilder;

        public enum CompareKind
        {
            EQ,
            NE,
            UGT,
            SGT,
            UGE,
            SGE,
            ULT,
            SLT,
            ULE,
            SLE,
        };

        readonly Dictionary<CompareKind, LLVMIntPredicate> _intPredicates = new Dictionary<CompareKind, LLVMIntPredicate>
        {
            [CompareKind.EQ] = LLVMIntPredicate.LLVMIntEQ,
            [CompareKind.NE] = LLVMIntPredicate.LLVMIntNE,
            [CompareKind.UGT] = LLVMIntPredicate.LLVMIntUGT,
            [CompareKind.SGT] = LLVMIntPredicate.LLVMIntSGT,
            [CompareKind.UGE] = LLVMIntPredicate.LLVMIntUGE,
            [CompareKind.SGE] = LLVMIntPredicate.LLVMIntSGE,
            [CompareKind.ULT] = LLVMIntPredicate.LLVMIntULT,
            [CompareKind.SLT] = LLVMIntPredicate.LLVMIntSLT,
            [CompareKind.ULE] = LLVMIntPredicate.LLVMIntULE,
            [CompareKind.SLE] = LLVMIntPredicate.LLVMIntSLE,
        };

        public CompilationBuilder(CompilationUnit compUnit, LLVMBuilderRef builder, CompilationFunction func, CompilationBlock block)
        {
            unit = compUnit;
            builderRef = builder;
            function = func;
            currentBlock = block;
        }

        public void PositionAtEnd(CompilationBlock block)
        {
            currentBlock = block;
            builderRef.PositionAtEnd(block.BackendValue);
        }

        public CompilationValue Load(CompilationValue loadFrom)
        {
            var loadedValue = new CompilationValue(builderRef.BuildLoad(loadFrom.BackendValue), loadFrom.Type, loadFrom.FrontendLocation);
            loadedValue.Storage = loadFrom.Storage;
            return loadedValue;
        }
        public CompilationValue Store(CompilationValue value, CompilationValue storeTo)
        {
            if (storeTo is CompilationValueOutputParameter compilationValueOutputParameter)
            {
                function.MarkUsed(compilationValueOutputParameter.Identifier);
            }
            return new CompilationValue(builderRef.BuildStore(value.BackendValue, storeTo.BackendValue), value.Type, value.FrontendLocation.Combine(storeTo.FrontendLocation));
        }

        public CompilationValue UDiv(CompilationValue left, CompilationValue right)
        {
            return new CompilationValue(builderRef.BuildUDiv(left.BackendValue, right.BackendValue), left.Type, left.FrontendLocation.Combine(right.FrontendLocation));
        }
        
        public CompilationValue SDiv(CompilationValue left, CompilationValue right)
        {
            return new CompilationValue(builderRef.BuildSDiv(left.BackendValue, right.BackendValue), left.Type, left.FrontendLocation.Combine(right.FrontendLocation));
        }

        public CompilationValue URem(CompilationValue left, CompilationValue right)
        {
            return new CompilationValue(builderRef.BuildURem(left.BackendValue, right.BackendValue), left.Type, left.FrontendLocation.Combine(right.FrontendLocation));
        }
        
        public CompilationValue SRem(CompilationValue left, CompilationValue right)
        {
            return new CompilationValue(builderRef.BuildSRem(left.BackendValue, right.BackendValue), left.Type, left.FrontendLocation.Combine(right.FrontendLocation));
        }

        public CompilationValue Mul(CompilationValue left, CompilationValue right)
        {
            return new CompilationValue(builderRef.BuildMul(left.BackendValue, right.BackendValue), left.Type, left.FrontendLocation.Combine(right.FrontendLocation));
        }
        
        public CompilationValue Add(CompilationValue left, CompilationValue right)
        {
            return new CompilationValue(builderRef.BuildAdd(left.BackendValue, right.BackendValue), left.Type, left.FrontendLocation.Combine(right.FrontendLocation));
        }

        public CompilationValue Sub(CompilationValue left, CompilationValue right)
        {
            return new CompilationValue(builderRef.BuildSub(left.BackendValue, right.BackendValue), left.Type, left.FrontendLocation.Combine(right.FrontendLocation));
        }

        public CompilationValue LogicalAnd(CompilationValue left, CompilationValue right)
        {
            return new CompilationValue(builderRef.BuildAnd(left.BackendValue, right.BackendValue), left.Type, left.FrontendLocation.Combine(right.FrontendLocation));
        }
        
        public CompilationValue LogicalOr(CompilationValue left, CompilationValue right)
        {
            return new CompilationValue(builderRef.BuildOr(left.BackendValue, right.BackendValue), left.Type, left.FrontendLocation.Combine(right.FrontendLocation));
        }

        public CompilationValue And(CompilationValue left, CompilationValue right)
        {
            return new CompilationValue(builderRef.BuildAnd(left.BackendValue, right.BackendValue), left.Type, left.FrontendLocation.Combine(right.FrontendLocation));
        }

        public CompilationValue Or(CompilationValue left, CompilationValue right)
        {
            return new CompilationValue(builderRef.BuildOr(left.BackendValue, right.BackendValue), left.Type, left.FrontendLocation.Combine(right.FrontendLocation));
        }

        public CompilationValue Xor(CompilationValue left, CompilationValue right)
        {
            return new CompilationValue(builderRef.BuildXor(left.BackendValue, right.BackendValue), left.Type, left.FrontendLocation.Combine(right.FrontendLocation));
        }

        public CompilationValue LogicalShiftLeft(CompilationValue left, CompilationValue right)
        {
            var leftInt = left.Type as CompilationIntegerType;
            var rightInt = right.Type as CompilationIntegerType;

            if (leftInt != null && rightInt != null)
            {
                var cNumBits = new CompilationConstantIntegerKind(new AstNumber($"{leftInt.IntegerWidth}"));
                var numBits = unit.CreateConstant(cNumBits, leftInt.IntegerWidth, false, new SourceLocation(left.FrontendLocation));
                var shiftAmount = URem(right, numBits);
                return ShiftLeft(left, shiftAmount);
            }
            throw new NotImplementedException($"Unhandled types in LogicalShiftLeft");
        }
        
        public CompilationValue LogicalShiftRight(CompilationValue left, CompilationValue right)
        {
            var leftInt = left.Type as CompilationIntegerType;
            var rightInt = right.Type as CompilationIntegerType;

            if (leftInt != null && rightInt != null)
            {
                var cNumBits = new CompilationConstantIntegerKind(new AstNumber($"{leftInt.IntegerWidth}"));
                var numBits = unit.CreateConstant(cNumBits, leftInt.IntegerWidth, false, new SourceLocation(left.FrontendLocation));
                var shiftAmount = URem(right, numBits);
                return ShiftRightLogical(left, shiftAmount);
            }
            throw new NotImplementedException($"Unhandled types in LogicalShiftRight");
        }
        public CompilationValue ArithmeticShiftRight(CompilationValue left, CompilationValue right)
        {
            var leftInt = left.Type as CompilationIntegerType;
            var rightInt = right.Type as CompilationIntegerType;

            if (leftInt != null && rightInt != null)
            {
                var cNumBits = new CompilationConstantIntegerKind(new AstNumber($"{leftInt.IntegerWidth}"));
                var numBits = unit.CreateConstant(cNumBits, leftInt.IntegerWidth, false, new SourceLocation(left.FrontendLocation));
                var shiftAmount = URem(right, numBits);
                return ShiftRightArithmetic(left, shiftAmount);
            }
            throw new NotImplementedException($"Unhandled types in LogicalShiftRight");
        }

        public CompilationValue Negate(CompilationValue src)
        {
            return new CompilationValue(builderRef.BuildNeg(src.BackendValue), src.Type, src.FrontendLocation);
        }

        public CompilationValue LogicalNot(CompilationValue src)
        {
            return new CompilationValue(builderRef.BuildNot(src.BackendValue), src.Type, src.FrontendLocation);
        }

        public CompilationValue Not(CompilationValue src)
        {
            return new CompilationValue(builderRef.BuildNot(src.BackendValue), src.Type, src.FrontendLocation);
        }

        public CompilationValue Alloca(CompilationType type)
        {
            return new CompilationValue(builderRef.BuildAlloca(type.BackendType), type, type.FrontendLocation);
        }

        public CompilationValue ExtractValue(CompilationValue src, CompilationType indexType, uint index)
        {
            return new CompilationValue(builderRef.BuildExtractValue(src.BackendValue, index), indexType, src.FrontendLocation);
        }
        
        public CompilationValue InsertValue(CompilationValue dst, CompilationValue toStore, uint index)
        {
            return new CompilationValue(builderRef.BuildInsertValue(dst.BackendValue, toStore.BackendValue, index), toStore.Type, dst.FrontendLocation.Combine(toStore.FrontendLocation));
        }

        public CompilationValue InBoundsGEP(CompilationValue ptr, CompilationPointerType resolvedType, LLVMValueRef[] indices)
        {
            var ptrType = ptr.Type as CompilationPointerType;
            if (ptrType==null)
                throw new System.ArgumentException($"GEP requires a pointer value");
            var value = new CompilationValue(builderRef.BuildInBoundsGEP(ptr.BackendValue, indices), resolvedType, ptr.FrontendLocation);
            value.Storage = value;
            return value;
        }

        public CompilationValue Ext(CompilationValue src, CompilationIntegerType srcType, CompilationIntegerType toType)
        {
            if (srcType.IsSigned)
                return new CompilationValue(builderRef.BuildSExt(src.BackendValue, toType.BackendType), toType, src.FrontendLocation);
            else
                return new CompilationValue(builderRef.BuildZExt(src.BackendValue, toType.BackendType), toType, src.FrontendLocation);
        }

        public CompilationValue Trunc(CompilationValue src, CompilationType toType)
        {
            var srcIntType = src.Type as CompilationIntegerType;
            var toIntType = toType as CompilationIntegerType;

            if (srcIntType != null && toIntType != null)
            {
                return new CompilationValue(builderRef.BuildTrunc(src.BackendValue, toType.BackendType), toType, src.FrontendLocation);
            }
            throw new NotImplementedException($"Unhandled type in truncate");
        }

        public CompilationValue MatchWidth(CompilationValue src, CompilationType toType)
        {
            var srcIntType = src.Type as CompilationIntegerType;
            var toIntType = toType as CompilationIntegerType;

            if (srcIntType != null && toIntType != null)
            {
                if (srcIntType.IntegerWidth == toIntType.IntegerWidth)
                    return src;
                else if (srcIntType.IntegerWidth > toIntType.IntegerWidth)
                    return Ext(src, srcIntType, toIntType);
                else
                    return Trunc(src, toType);
            }
            throw new NotImplementedException($"Unhandled type in match width");
        }

        public CompilationValue Cast(CompilationValue src, CompilationType toType)
        {
            var sType = src.Type;
            var dType = toType;

            if (sType is CompilationEnumType set)
                sType = set.ElementType;
            if (dType is CompilationEnumType det)
                dType = det.ElementType;

            if (sType is CompilationIntegerType sit && dType is CompilationIntegerType tt)
            {
                if (sit.IntegerWidth > tt.IntegerWidth)
                    return new CompilationValue(builderRef.BuildTrunc(src.BackendValue, dType.BackendType), dType, src.FrontendLocation);
                else
                {
                    if (tt.IsSigned)
                        return new CompilationValue(builderRef.BuildSExt(src.BackendValue, dType.BackendType), dType, src.FrontendLocation);
                    else
                        return new CompilationValue(builderRef.BuildZExt(src.BackendValue, dType.BackendType), dType, src.FrontendLocation);
                }
            }
            if (dType is CompilationFunctionType cft)
                dType = unit.CreatePointerType(cft,cft.Location);                 
            if (sType is CompilationPointerType && dType is CompilationIntegerType)
                return new CompilationValue(builderRef.BuildPtrToInt(src.BackendValue, dType.BackendType), dType, src.FrontendLocation);
            if (sType is CompilationIntegerType && dType is CompilationPointerType)
                return new CompilationValue(builderRef.BuildIntToPtr(src.BackendValue, dType.BackendType), dType, src.FrontendLocation);

            return new CompilationValue(builderRef.BuildBitCast(src.BackendValue, dType.BackendType), dType, src.FrontendLocation);
        }
        
        public CompilationValue Compare(CompareKind compareKind, CompilationValue left, CompilationValue right)
        {
            if (_intPredicates.TryGetValue(compareKind, out var intPredicate))
                return new CompilationValue(builderRef.BuildICmp(intPredicate, left.BackendValue, right.BackendValue),
                    unit.CreateIntegerType(1, false, new SourceLocation()), left.FrontendLocation.Combine(right.FrontendLocation));

            throw new NotImplementedException($"Unahandled compare kind {compareKind}");
        }

        public CompilationValue Select(CompilationValue compare, CompilationValue trueValue, CompilationValue falseValue)
        {
            var location = compare.FrontendLocation.Combine(trueValue.FrontendLocation).Combine(falseValue.FrontendLocation);
            return new CompilationValue(builderRef.BuildSelect(compare.BackendValue, trueValue.BackendValue, falseValue.BackendValue), trueValue.Type, location);
        }

        // Brings in 0s
        public CompilationValue ShiftLeft(CompilationValue toShift, CompilationValue shiftAmount)
        {
            return new CompilationValue(builderRef.BuildShl(toShift.BackendValue, shiftAmount.BackendValue), toShift.Type, toShift.FrontendLocation.Combine(shiftAmount.FrontendLocation));
        }
        public CompilationValue ShiftRightLogical(CompilationValue toShift, CompilationValue shiftAmount)
        {
            return new CompilationValue(builderRef.BuildLShr(toShift.BackendValue, shiftAmount.BackendValue), toShift.Type, toShift.FrontendLocation.Combine(shiftAmount.FrontendLocation));
        }
        public CompilationValue ShiftRightArithmetic(CompilationValue toShift, CompilationValue shiftAmount)
        {
            return new CompilationValue(builderRef.BuildAShr(toShift.BackendValue, shiftAmount.BackendValue), toShift.Type, toShift.FrontendLocation.Combine(shiftAmount.FrontendLocation));
        }

        public CompilationValue RotateLeft(CompilationValue value, CompilationValue rotateBy)
        {
            var backendType = value.BackendType;
            var funnelShift = unit.FetchIntrinsicFunction("llvm.fshl", new LLVMTypeRef[] { backendType });
            var backendValues = new LLVMValueRef[3];
            backendValues[0] = value.BackendValue;
            backendValues[1] = value.BackendValue;
            backendValues[2] = rotateBy.BackendValue;
            return new CompilationValue(builderRef.BuildCall(funnelShift, backendValues), value.Type, value.FrontendLocation.Combine(rotateBy.FrontendLocation));
        }

        public CompilationValue RotateRight(CompilationValue value, CompilationValue rotateBy)
        {
            var backendType = value.BackendType;
            var funnelShift = unit.FetchIntrinsicFunction("llvm.fshr", new LLVMTypeRef[] { backendType });
            var backendValues = new LLVMValueRef[3];
            backendValues[0] = value.BackendValue;
            backendValues[1] = value.BackendValue;
            backendValues[2] = rotateBy.BackendValue;
            return new CompilationValue(builderRef.BuildCall(funnelShift, backendValues), value.Type, value.FrontendLocation.Combine(rotateBy.FrontendLocation));
        }

        public CompilationValue Call(CompilationValue func, CompilationValue[] arguments)
        {
            var backendValues = new LLVMValueRef[arguments.Length];
            for (int a = 0; a < arguments.Length; a++)
            {
                backendValues[a] = arguments[a].BackendValue;
            }

            var returnKind = (func.Type as CompilationFunctionType).ReturnType;

            var res=builderRef.BuildCall(func.BackendValue, backendValues);
            if (returnKind==null)
                return null;
            return new CompilationValue(res, returnKind.Type, func.FrontendLocation);
        }

        public void Branch(CompilationBlock destinationBlock)
        {
            builderRef.BuildBr(destinationBlock.BackendValue);
        }

        public void ConditionalBranch(CompilationValue cond, CompilationBlock trueBlock, CompilationBlock falseBlock)
        {
            builderRef.BuildCondBr(cond.BackendValue, trueBlock.BackendValue, falseBlock.BackendValue);
        }

        public void ReturnVoid()
        {
            builderRef.BuildRetVoid();
        }

        public void SetDebugLocation(SourceLocation location)
        {
            if (unit.DebugInfoEnabled)
            {
                builderRef.CurrentDebugLocation = unit.CreateDebugLocation(location);
            }
        }

        public CompilationBuilder LocalBuilder 
        {
            get => localsBuilder;
            set => localsBuilder = value;
        }
        public LLVMBuilderRef BackendValue => builderRef;
        public CompilationFunction Function => function;
        public CompilationBlock CurrentBlock => currentBlock;
    }
}
