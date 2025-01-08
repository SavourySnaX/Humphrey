using System;
using System.Collections.Generic;
using Extensions;
using Humphrey.Compiler.src.Backend.ABI;
using Humphrey.FrontEnd;
using LLVMSharp;
using LLVMSharp.Interop;

namespace Humphrey.Backend
{
    public class CompilationBuilder
    {
        CompilationUnit unit;
        LLVMBuilderRef builderRef;
        CompilationFunction function;
        CompilationBlock currentBlock;
        LLVMBuilderRef localsBuilder;

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

        readonly Dictionary<CompareKind, LLVMRealPredicate> _floatPredicates = new Dictionary<CompareKind, LLVMRealPredicate>
        {
            [CompareKind.EQ] = LLVMRealPredicate.LLVMRealOEQ,
            [CompareKind.NE] = LLVMRealPredicate.LLVMRealONE,
            [CompareKind.SGT] = LLVMRealPredicate.LLVMRealOGT,
            [CompareKind.SGE] = LLVMRealPredicate.LLVMRealOGE,
            [CompareKind.SLT] = LLVMRealPredicate.LLVMRealOLT,
            [CompareKind.SLE] = LLVMRealPredicate.LLVMRealOLE,
        };

        public CompilationBuilder(CompilationUnit compUnit, LLVMBuilderRef builder, CompilationFunction func, CompilationBlock block, LLVMBuilderRef locals)
        {
            unit = compUnit;
            builderRef = builder;
            function = func;
            currentBlock = block;
            this.localsBuilder = locals;
        }

        public void PositionAtEnd(CompilationBlock block)
        {
            currentBlock = block;
            builderRef.PositionAtEnd(block.BackendValue);
        }

        public void SetAlignment(CompilationValue value, CompilationValue from)
        {
            var alignment = from.BackendValue.Alignment;
            if (from.Alignment != 0 && alignment == 0)
            {
                alignment = from.Alignment;
            }
            if (alignment == 0)
                return;
            value.BackendValue.SetAlignment(alignment);
        }

        public CompilationValue Load(CompilationType loadType, CompilationValue loadFrom)
        {
            var loadedValue = new CompilationValue(builderRef.BuildLoad2(loadType.BackendType, loadFrom.BackendValue), loadFrom.Type, loadFrom.FrontendLocation);
            loadedValue.Storage = loadFrom.Storage;
            SetAlignment(loadedValue, loadFrom);
            return loadedValue;
        }

        public CompilationValue LoadAtomic(CompilationType loadType, CompilationValue loadFrom, AtomicOrdering ordering)
        {
            var loadedValue = new CompilationValue(builderRef.BuildLoad2(loadType.BackendType, loadFrom.BackendValue), loadType, loadFrom.FrontendLocation);
            loadedValue.BackendValue.SetOrdering((LLVMAtomicOrdering)ordering);
            loadedValue.Storage = loadFrom;
            SetAlignment(loadedValue, loadFrom);
            return loadedValue;
        }

        public CompilationValue Store(CompilationValue value, CompilationValue storeTo)
        {
            if (storeTo is CompilationValueOutputParameter compilationValueOutputParameter)
            {
                function.MarkUsed(compilationValueOutputParameter.Identifier);
            }
            var store = new CompilationValue(builderRef.BuildStore(value.BackendValue, storeTo.BackendValue), value.Type, value.FrontendLocation.Combine(storeTo.FrontendLocation));
            SetAlignment(store, storeTo);
            return store;
        }

        public CompilationValue StoreAtomic(CompilationValue value, CompilationValue storeTo, AtomicOrdering ordering)
        {
            if (storeTo is CompilationValueOutputParameter compilationValueOutputParameter)
            {
                function.MarkUsed(compilationValueOutputParameter.Identifier);
            }
            var storeValue = new CompilationValue(builderRef.BuildStore(value.BackendValue, storeTo.BackendValue), value.Type, value.FrontendLocation.Combine(storeTo.FrontendLocation));
            storeValue.BackendValue.SetOrdering((LLVMAtomicOrdering)ordering);
            SetAlignment(storeValue, storeTo);
            return storeValue;
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

        public CompilationValue FAdd(CompilationValue left, CompilationValue right)
        {
            return new CompilationValue(builderRef.BuildFAdd(left.BackendValue, right.BackendValue), left.Type, left.FrontendLocation.Combine(right.FrontendLocation));
        }

        public CompilationValue FSub(CompilationValue left, CompilationValue right)
        {
            return new CompilationValue(builderRef.BuildFSub(left.BackendValue, right.BackendValue), left.Type, left.FrontendLocation.Combine(right.FrontendLocation));
        }
        
        public CompilationValue FMul(CompilationValue left, CompilationValue right)
        {
            return new CompilationValue(builderRef.BuildFMul(left.BackendValue, right.BackendValue), left.Type, left.FrontendLocation.Combine(right.FrontendLocation));
        }

        public CompilationValue FDiv(CompilationValue left, CompilationValue right)
        {
            return new CompilationValue(builderRef.BuildFDiv(left.BackendValue, right.BackendValue), left.Type, left.FrontendLocation.Combine(right.FrontendLocation));
        }

        public CompilationValue FRem(CompilationValue left, CompilationValue right)
        {
            return new CompilationValue(builderRef.BuildFRem(left.BackendValue, right.BackendValue), left.Type, left.FrontendLocation.Combine(right.FrontendLocation));
        }

        public CompilationValue SignedToFloat(CompilationValue src, CompilationType destType)
        {
            return new CompilationValue(builderRef.BuildSIToFP(src.BackendValue, destType.BackendType), destType, src.FrontendLocation);
        }

        public CompilationValue UnsignedToFloat(CompilationValue src, CompilationType destType)
        {
            return new CompilationValue(builderRef.BuildUIToFP(src.BackendValue, destType.BackendType), destType, src.FrontendLocation);
        }

        public CompilationValue FloatToSigned(CompilationValue src, CompilationType destType)
        {
            return new CompilationValue(builderRef.BuildFPToSI(src.BackendValue, destType.BackendType), destType, src.FrontendLocation);
        }

        public CompilationValue FloatToUnsigned(CompilationValue src, CompilationType destType)
        {
            return new CompilationValue(builderRef.BuildFPToUI(src.BackendValue, destType.BackendType), destType, src.FrontendLocation);
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
            var leftIntType = left.Type as CompilationIntegerType;
            var rightIntType = right.Type as CompilationIntegerType;

            if (left.Type is CompilationEnumType lcet)
                leftIntType = lcet.ElementType as CompilationIntegerType;
            if (right.Type is CompilationEnumType rcet)
                rightIntType = rcet.ElementType as CompilationIntegerType;

            if (leftIntType != null && rightIntType != null)
            {
                var cNumBits = new CompilationConstantIntegerKind(new AstNumber($"{leftIntType.IntegerWidth}"));
                var numBits = unit.CreateConstant(cNumBits, leftIntType.IntegerWidth, false, new SourceLocation(left.FrontendLocation));
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
            if (src.Type is CompilationFloatType)
                return new CompilationValue(builderRef.BuildFNeg(src.BackendValue), src.Type, src.FrontendLocation);
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

        public CompilationValue Alloca(CompilationType type, string name)
        {
            return new CompilationValue(builderRef.CreateAlloca(localsBuilder, type.BackendType, name), type, type.FrontendLocation);
        }

        public CompilationValue Alloca(CompilationType type)
        {
            return new CompilationValue(builderRef.CreateAlloca(localsBuilder, type.BackendType, "alloca"), type, type.FrontendLocation);
        }

        public CompilationValue ExtractValue(CompilationValue src, CompilationType indexType, uint index)
        {
            return new CompilationValue(builderRef.BuildExtractValue(src.BackendValue, index), indexType, src.FrontendLocation);
        }
        
        public CompilationValue InsertValue(CompilationValue dst, CompilationValue toStore, uint index)
        {
            return new CompilationValue(builderRef.BuildInsertValue(dst.BackendValue, toStore.BackendValue, index), toStore.Type, dst.FrontendLocation.Combine(toStore.FrontendLocation));
        }

        public CompilationValue InBoundsGEP(CompilationType type, CompilationValue ptr, CompilationPointerType resolvedType, LLVMValueRef[] indices, uint alignment = 0)
        {
            if (ptr==null)
                throw new System.ArgumentException($"GEP requires a pointer value");
            var ptrType = ptr.Type as CompilationPointerType;
            if (ptrType==null)
                throw new System.ArgumentException($"GEP requires a pointer value");
            var value = new CompilationValue(builderRef.BuildInBoundsGEP2(type.BackendType, ptr.BackendValue, indices), resolvedType, ptr.FrontendLocation);
            value.Storage = value;
            value.Alignment = alignment;
            return value;
        }

        public CompilationValue Ext(CompilationValue src, CompilationIntegerType toType)
        {
            var srcIntType = src.Type as CompilationIntegerType;
            var srcEnumType = src.Type as CompilationEnumType;
            if (srcEnumType != null)
            {
                srcIntType = srcEnumType.ElementType as CompilationIntegerType;
            }

            if (srcIntType != null)
            {
                if (srcIntType.IsSigned)
                    return new CompilationValue(builderRef.BuildSExt(src.BackendValue, toType.BackendType), toType, src.FrontendLocation);
                else
                    return new CompilationValue(builderRef.BuildZExt(src.BackendValue, toType.BackendType), toType, src.FrontendLocation);
            }
            throw new NotImplementedException($"Unhandled type in ext");
        }

        public CompilationValue Trunc(CompilationValue src, CompilationIntegerType toType)
        {
            var srcIntType = src.Type as CompilationIntegerType;
            var srcEnumType = src.Type as CompilationEnumType;
            if (srcEnumType != null)
            {
                srcIntType = srcEnumType.ElementType as CompilationIntegerType;
            }

            if (srcIntType != null)
            {
                return new CompilationValue(builderRef.BuildTrunc(src.BackendValue, toType.BackendType), toType, src.FrontendLocation);
            }
            throw new NotImplementedException($"Unhandled type in truncate");
        }

        public CompilationValue MatchWidth(CompilationValue src, CompilationType toType)
        {
            var srcIntType = src.Type as CompilationIntegerType;
            if (src.Type is CompilationEnumType compilationEnumType)
                srcIntType = compilationEnumType.ElementType as CompilationIntegerType;

            var toIntType = toType as CompilationIntegerType;

            if (srcIntType != null && toIntType != null)
            {
                if (srcIntType.IntegerWidth == toIntType.IntegerWidth)
                    return src;
                else if (srcIntType.IntegerWidth > toIntType.IntegerWidth)
                    return Trunc(src, toIntType);
                else
                    return Ext(src, toIntType);
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
            if (sType is CompilationFunctionType && dType is CompilationIntegerType)
                return new CompilationValue(builderRef.BuildPtrToInt(src.BackendValue, dType.BackendType), dType, src.FrontendLocation);
            if (sType is CompilationIntegerType && dType is CompilationPointerType)
                return new CompilationValue(builderRef.BuildIntToPtr(src.BackendValue, dType.BackendType), dType, src.FrontendLocation);

            var cast = new CompilationValue(builderRef.BuildBitCast(src.BackendValue, dType.BackendType), dType, src.FrontendLocation);
            cast.Alignment = src.Alignment;
            return cast;
        }
        
        public CompilationValue FCompare(CompareKind comparekind,  CompilationValue lhs, CompilationValue rhs)
        {
            if (_floatPredicates.TryGetValue(comparekind, out var predicate))
                return new CompilationValue(builderRef.BuildFCmp(predicate, lhs.BackendValue, rhs.BackendValue),
                    unit.CreateIntegerType(1, false, new SourceLocation()), lhs.FrontendLocation.Combine(rhs.FrontendLocation));

            throw new NotImplementedException($"Unahandled compare kind {comparekind}");
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
            var funnelShiftType = unit.FetchIntrinsicFunctionType("llvm.fshl", new LLVMTypeRef[] { backendType });
            var backendValues = new LLVMValueRef[3];
            backendValues[0] = value.BackendValue;
            backendValues[1] = value.BackendValue;
            backendValues[2] = rotateBy.BackendValue;
            return new CompilationValue(builderRef.BuildCall2(funnelShiftType,funnelShift, backendValues), value.Type, value.FrontendLocation.Combine(rotateBy.FrontendLocation));
        }

        public CompilationValue RotateRight(CompilationValue value, CompilationValue rotateBy)
        {
            var backendType = value.BackendType;
            var funnelShift = unit.FetchIntrinsicFunction("llvm.fshr", new LLVMTypeRef[] { backendType });
            var funnelShiftType = unit.FetchIntrinsicFunctionType("llvm.fshr", new LLVMTypeRef[] { backendType });
            var backendValues = new LLVMValueRef[3];
            backendValues[0] = value.BackendValue;
            backendValues[1] = value.BackendValue;
            backendValues[2] = rotateBy.BackendValue;
            return new CompilationValue(builderRef.BuildCall2(funnelShiftType, funnelShift, backendValues), value.Type, value.FrontendLocation.Combine(rotateBy.FrontendLocation));
        }

        public CompilationValue Call(CompilationValue func, CompilationValue[] arguments)
        {
            var compilationFunctionType = func.Type as CompilationFunctionType;

            var backendValues = new LLVMValueRef[arguments.Length];
            for (int a = 0; a < arguments.Length; a++)
            {
                backendValues[a] = arguments[a].BackendValue;
            }

            var returnKind = compilationFunctionType.ReturnType;

            if (compilationFunctionType.FunctionCallingConvention == CompilationFunctionType.CallingConvention.CDecl)
            {
                var argInfo = unit.TargetABI.ComputeTransform(unit, compilationFunctionType);
                var mapping = unit.TargetABI.GetFunctionIRMapping(argInfo);

                var caller = new Caller(unit.TargetABI, func.Type.BackendType, func.BackendValue, backendValues, mapping, builderRef, localsBuilder, this.unit);

                var encodedArguments = caller.encodeArguments(compilationFunctionType).ToArray();

                var returnValue = builderRef.BuildCall2(func.Type.BackendType, func.BackendValue, encodedArguments);
                if (returnKind == null)
                    return null;

                var (converted, storage) = caller.decodeReturnValue(encodedArguments, returnValue, returnKind.Type.BackendType);
                var cv = new CompilationValue(converted, returnKind.Type, func.FrontendLocation);
                if (storage != null)
                {
                    cv.Storage = new CompilationValue(storage, unit.CreatePointerType(returnKind.Type, new SourceLocation(func.FrontendLocation)), func.FrontendLocation);
                }
                return cv;
            }

            var res=builderRef.BuildCall2(func.Type.BackendType, func.BackendValue, backendValues);
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

        // Intrinsic vector support below (types are not reflected back into humphrey types
        public LLVMValueRef FAdd(LLVMValueRef left, LLVMValueRef right)
        {
            return builderRef.BuildFAdd(left, right);
        }
        public LLVMValueRef FSub(LLVMValueRef left, LLVMValueRef right)
        {
            return builderRef.BuildFSub(left, right);
        }
        public LLVMValueRef FMul(LLVMValueRef left, LLVMValueRef right)
        {
            return builderRef.BuildFMul(left, right);
        }
        public LLVMValueRef FDiv(LLVMValueRef left, LLVMValueRef right)
        {
            return builderRef.BuildFDiv(left, right);
        }

        public LLVMValueRef DoIntrinsic(string name, LLVMTypeRef[] backendTypes, LLVMValueRef[] backendValues)
        {
            var function = unit.FetchIntrinsicFunction(name, backendTypes);
            var functionType = unit.FetchIntrinsicFunctionType(name, backendTypes);
            return builderRef.BuildCall2(functionType, function, backendValues);
        }

        public LLVMValueRef CreateConstF(float v)
        {
            var t = unit.Context.FloatType;
            return t.CreateConstantFloatValue(v);
        }
        public LLVMValueRef FDot(LLVMValueRef left, LLVMValueRef right)
        {
            var r = FMul(left, right);
            return DoIntrinsic("llvm.vector.reduce.fadd", new[] { left.TypeOf }, new[] { CreateConstF(-0.0f), r });
        }

        public LLVMValueRef FFloor(LLVMValueRef left)
        {
            return DoIntrinsic("llvm.floor", new[] { left.TypeOf }, new[] { left });
        }

        public unsafe LLVMValueRef StructToVec(CompilationValue input, uint numElements)
        {
            if (input.BackendValue.TypeOf.Kind == LLVMTypeKind.LLVMVectorTypeKind)
            {
                return input.BackendValue;
            }
            var elementType = (input.Type as CompilationStructureType).Elements[0].BackendType;
            var t = LLVM.VectorType(elementType, numElements);
            var v = LLVM.GetUndef(t);
            var r = v;
            for (uint a=0;a<numElements;a++)
            {
                var sElement = builderRef.BuildExtractValue(input.BackendValue, a);
                var idx = unit.CreateI64Constant(a);
                r = builderRef.BuildInsertElement(r, sElement, idx);
            }
            return r;
        }

        public unsafe CompilationValue VecToStruct(LLVMValueRef input, CompilationType t, uint numElements, Result<Tokens> frontendLocation)
        {
            var r = LLVM.GetUndef(t.BackendType);
            for (uint a = 0; a < numElements; a++)
            {
                var idx = unit.CreateI64Constant(a);
                var element = builderRef.BuildExtractElement(input, idx);
                r = builderRef.BuildInsertValue(r, element, a);
            }
            var alloc = Alloca(t);
            var rv = new CompilationValue(r, t, frontendLocation);
            Store(rv, alloc);
            rv.Storage = alloc;
            return rv;
        }

        public unsafe CompilationValue FloatTo(LLVMValueRef input, CompilationType t, Result<Tokens> frontendLocation)
        {
            var alloc = Alloca(t);
            var rv = new CompilationValue(input, t, frontendLocation);
            Store(rv, alloc);
            rv.Storage = alloc;
            return rv;
        }

        public void SetDebugLocation(SourceLocation location)
        {
            if (unit.DebugInfoEnabled)
            {
                builderRef.CurrentDebugLocation = unit.CreateDebugLocation(location);
            }
        }

        public LLVMBuilderRef BackendValue => builderRef;
        public CompilationFunction Function => function;
        public CompilationBlock CurrentBlock => currentBlock;

        public CompilationUnit Unit => unit;
        public LLVMBuilderRef Locals => localsBuilder;
    }
}
