using System;
using System.Collections.Generic;
using LLVMSharp.Interop;

namespace Humphrey.Backend
{
    public class CompilationBuilder
    {
        LLVMBuilderRef builderRef;
        CompilationFunction function;
        CompilationBlock currentBlock;

        public enum CompareKind
        {
            ULT,
            SLT,
        };

        readonly Dictionary<CompareKind, LLVMIntPredicate> _intPredicates = new Dictionary<CompareKind, LLVMIntPredicate>
        {
            [CompareKind.ULT] = LLVMIntPredicate.LLVMIntULT,
            [CompareKind.SLT] = LLVMIntPredicate.LLVMIntSLT,
        };

        public CompilationBuilder(LLVMBuilderRef builder, CompilationFunction func, CompilationBlock block)
        {
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
            var loadedValue = new CompilationValue(builderRef.BuildLoad(loadFrom.BackendValue), loadFrom.Type);
            loadedValue.Storage = loadFrom.Storage;
            return loadedValue;
        }
        public CompilationValue Store(CompilationValue value, CompilationValue storeTo)
        {
            return new CompilationValue(builderRef.BuildStore(value.BackendValue, storeTo.BackendValue), value.Type);
        }

        public CompilationValue UDiv(CompilationValue left, CompilationValue right)
        {
            return new CompilationValue(builderRef.BuildUDiv(left.BackendValue, right.BackendValue), left.Type);
        }
        
        public CompilationValue SDiv(CompilationValue left, CompilationValue right)
        {
            return new CompilationValue(builderRef.BuildSDiv(left.BackendValue, right.BackendValue), left.Type);
        }

        public CompilationValue URem(CompilationValue left, CompilationValue right)
        {
            return new CompilationValue(builderRef.BuildURem(left.BackendValue, right.BackendValue), left.Type);
        }
        
        public CompilationValue SRem(CompilationValue left, CompilationValue right)
        {
            return new CompilationValue(builderRef.BuildSRem(left.BackendValue, right.BackendValue), left.Type);
        }

        public CompilationValue Mul(CompilationValue left, CompilationValue right)
        {
            return new CompilationValue(builderRef.BuildMul(left.BackendValue, right.BackendValue, "".AsSpan()), left.Type);
        }
        
        public CompilationValue Add(CompilationValue left, CompilationValue right)
        {
            return new CompilationValue(builderRef.BuildAdd(left.BackendValue, right.BackendValue), left.Type);
        }

        public CompilationValue Sub(CompilationValue left, CompilationValue right)
        {
            return new CompilationValue(builderRef.BuildSub(left.BackendValue, right.BackendValue), left.Type);
        }

        public CompilationValue Negate(CompilationValue src)
        {
            return new CompilationValue(builderRef.BuildNeg(src.BackendValue), src.Type);
        }

        public CompilationValue Alloca(CompilationType type)
        {
            return new CompilationValue(builderRef.BuildAlloca(type.BackendType), type);
        }

        public CompilationValue ExtractValue(CompilationValue src, CompilationType indexType, uint index)
        {
            return new CompilationValue(builderRef.BuildExtractValue(src.BackendValue, index), indexType);
        }
        
        public CompilationValue InsertValue(CompilationValue dst, CompilationValue toStore, uint index)
        {
            return new CompilationValue(builderRef.BuildInsertValue(dst.BackendValue, toStore.BackendValue, index), toStore.Type);
        }


        public CompilationValue InBoundsGEP(CompilationValue ptr, LLVMValueRef[] indices)
        {
            var ptrType = ptr.Type as CompilationPointerType;
            if (ptrType==null)
                throw new System.ArgumentException($"GEP requires a pointer value");
            return new CompilationValue(builderRef.BuildInBoundsGEP(ptr.BackendValue, indices), ptrType);
        }

        public CompilationValue Ext(CompilationValue src, CompilationType toType)
        {
            var srcIntType = src.Type as CompilationIntegerType;
            var toIntType = toType as CompilationIntegerType;

            if (srcIntType != null && toIntType != null)
            {
                if (srcIntType.IsSigned)
                    return new CompilationValue(builderRef.BuildSExt(src.BackendValue,toType.BackendType), toType);
                else
                    return new CompilationValue(builderRef.BuildZExt(src.BackendValue,toType.BackendType), toType);
            }
            throw new NotImplementedException($"Unhandled type in extension");
        }

        public CompilationValue Compare(CompareKind compareKind, CompilationValue left, CompilationValue right)
        {
            if (_intPredicates.TryGetValue(compareKind, out var intPredicate))
                return new CompilationValue(builderRef.BuildICmp(intPredicate, left.BackendValue, right.BackendValue), new CompilationIntegerType(Extensions.Helpers.CreateIntType(1), false));

            throw new NotImplementedException($"Unahandled compare kind {compareKind}");
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

        public LLVMBuilderRef BackendValue => builderRef;
        public CompilationFunction Function => function;
        public CompilationBlock CurrentBlock => currentBlock;
    }
}
