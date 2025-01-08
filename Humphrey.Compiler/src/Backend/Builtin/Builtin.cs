using Humphrey.Backend;
using LLVMSharp.Interop;
using System;

namespace Humphrey.Compiler.src.Backend.Builtin
{
    internal static partial class Builtin
    {
        private static LLVMSharp.AtomicOrdering GetAtomicOrdering(CompilationValue value)
        {
            if (value.Type is CompilationIntegerType cit)
            {
                if (value.BackendValue.IsConstant)
                {
                    var idx = value.BackendValue.ConstIntZExt;
                    switch ((int)idx)
                    {
                        case 0:
                            return LLVMSharp.AtomicOrdering.Unordered;
                        case 1:
                            return LLVMSharp.AtomicOrdering.Monotonic;
                        case 2:
                            return LLVMSharp.AtomicOrdering.Acquire;
                        case 3:
                            return LLVMSharp.AtomicOrdering.Release;
                        case 4:
                            return LLVMSharp.AtomicOrdering.AcquireRelease;
                        case 5:
                            return LLVMSharp.AtomicOrdering.SequentiallyConsistent;
                    }
                }
            }
            throw new CompilationAbortException($"Atomic ordering must be a constant integer value");
        }

        static bool IsIntrinsicTypeCorrect(CompilationValue v, int numElements, LLVMTypeKind elementType)
        {
            if (v.Type is CompilationStructureType cst)
            {
                foreach (var e in cst.Elements)
                {
                    if (e.BackendType.Kind == elementType)
                    {
                        numElements--;
                    }
                }

                if (numElements == 0)
                    return true;
            }
            return false;
        }

        public static ICompilationValue CallBuiltIn(CompilationUnit unit, CompilationBuilder builder, CompilationValue function, CompilationFunctionType ftype, CompilationValue[] inputs)
        {
            switch (ftype.Identifier)
            {
                case "Intrinsic_AtomicLoadExplicit":
                    {
                        return builder.LoadAtomic(ftype.ReturnType.Type, inputs[0], GetAtomicOrdering(inputs[1]));
                    }
                case "Intrinsic_AtomicStoreExplicit":
                    {
                        builder.StoreAtomic(inputs[1], inputs[0], GetAtomicOrdering(inputs[2]));
                        return inputs[1];
                    }
                case "Intrinsic_Vec2FAdd":
                case "Intrinsic_Vec2FSub":
                case "Intrinsic_Vec2FMul":
                case "Intrinsic_Vec2FDiv":
                    {
                        // Step 2 validate our inputs are expected
                        if (IsIntrinsicTypeCorrect(inputs[0], 2, LLVMTypeKind.LLVMFloatTypeKind) && IsIntrinsicTypeCorrect(inputs[1], 2, LLVMTypeKind.LLVMFloatTypeKind))
                        {
                            var vecA = builder.StructToVec(inputs[0], 2);
                            var vecB = builder.StructToVec(inputs[1], 2);
                            LLVMValueRef res;

                            switch (ftype.Identifier)
                            {
                                case "Intrinsic_Vec2FAdd":
                                    res = builder.FAdd(vecA, vecB);
                                    break;
                                case "Intrinsic_Vec2FSub":
                                    res = builder.FSub(vecA, vecB);
                                    break;
                                case "Intrinsic_Vec2FMul":
                                    res = builder.FMul(vecA, vecB);
                                    break;
                                case "Intrinsic_Vec2FDiv":
                                    res = builder.FDiv(vecA, vecB);
                                    break;
                                default:
                                    throw new NotImplementedException($"Built in function {ftype.Identifier} not implemented");
                            }

                            return builder.VecToStruct(res, inputs[0].Type, 2, function.FrontendLocation);
                        }

                        throw new Exception($"Built in function (Vec2FAdd types mismatch) - Something is wrong");
                    }
                case "Intrinsic_Vec2FDot":
                    {
                        if (IsIntrinsicTypeCorrect(inputs[0], 2, LLVMTypeKind.LLVMFloatTypeKind) && IsIntrinsicTypeCorrect(inputs[1], 2, LLVMTypeKind.LLVMFloatTypeKind))
                        {
                            var vecA = builder.StructToVec(inputs[0], 2);
                            var vecB = builder.StructToVec(inputs[1], 2);
                            LLVMValueRef res;

                            res =  builder.FDot(vecA, vecB);

                            return builder.FloatTo(res, (inputs[0].Type as CompilationStructureType).Elements[0], function.FrontendLocation);
                        }

                        throw new Exception($"Built in function (Vec2FAdd types mismatch) - Something is wrong");
                    }

            }
            throw new NotImplementedException($"Built in function {ftype.Identifier} not implemented");
        }

    }
}
