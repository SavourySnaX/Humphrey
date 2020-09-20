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
            return new CompilationValue(builderRef.BuildMul(left.BackendValue, right.BackendValue, "".AsSpan()), left.Type, left.FrontendLocation.Combine(right.FrontendLocation));
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
            return new CompilationValue(builderRef.BuildInBoundsGEP(ptr.BackendValue, indices), resolvedType, ptr.FrontendLocation);
        }

        public CompilationValue Ext(CompilationValue src, CompilationType toType)
        {
            var srcIntType = src.Type as CompilationIntegerType;
            var toIntType = toType as CompilationIntegerType;

            if (srcIntType != null && toIntType != null)
            {
                if (srcIntType.IsSigned)
                    return new CompilationValue(builderRef.BuildSExt(src.BackendValue,toType.BackendType), toType, src.FrontendLocation);
                else
                    return new CompilationValue(builderRef.BuildZExt(src.BackendValue,toType.BackendType), toType, src.FrontendLocation);
            }
            throw new NotImplementedException($"Unhandled type in extension");
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
                    return Ext(src, toType);
                else
                    return Trunc(src, toType);
            }
            throw new NotImplementedException($"Unhandled type in match width");
        }

        public CompilationValue Cast(CompilationValue src, CompilationType toType)
        {

            if (src.Type is CompilationIntegerType sit && toType is CompilationIntegerType tt)
            {
                if (sit.IntegerWidth > tt.IntegerWidth)
                    return new CompilationValue(builderRef.BuildTrunc(src.BackendValue, toType.BackendType), toType, src.FrontendLocation);
                else
                {
                    if (tt.IsSigned)
                        return new CompilationValue(builderRef.BuildSExt(src.BackendValue, toType.BackendType), toType, src.FrontendLocation);
                    else
                        return new CompilationValue(builderRef.BuildZExt(src.BackendValue, toType.BackendType), toType, src.FrontendLocation);
                }
            }
            if (toType is CompilationFunctionType cft)
                toType = unit.CreatePointerType(cft,cft.Location);                 
            if (src.Type is CompilationPointerType && toType is CompilationIntegerType)
                return new CompilationValue(builderRef.BuildPtrToInt(src.BackendValue, toType.BackendType), toType, src.FrontendLocation);
            if (src.Type is CompilationIntegerType && toType is CompilationPointerType)
                return new CompilationValue(builderRef.BuildIntToPtr(src.BackendValue, toType.BackendType), toType, src.FrontendLocation);

            return new CompilationValue(builderRef.BuildBitCast(src.BackendValue, toType.BackendType), toType, src.FrontendLocation);
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

        public void Call(CompilationValue func, CompilationValue[] arguments)
        {
            var backendValues = new LLVMValueRef[arguments.Length];
            for (int a = 0; a < arguments.Length; a++)
            {
                backendValues[a] = arguments[a].BackendValue;
            }

            builderRef.BuildCall(func.BackendValue, backendValues);
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
            builderRef.CurrentDebugLocation = unit.CreateDebugLocation(location);
        }

        public LLVMBuilderRef BackendValue => builderRef;
        public CompilationFunction Function => function;
        public CompilationBlock CurrentBlock => currentBlock;
    }
}
