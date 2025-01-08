using Humphrey.Backend;
using Humphrey.FrontEnd;
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

        static bool IsIntrinsicTypeCorrect(CompilationValue v, uint numElements, LLVMTypeKind elementType)
        {
            if (v.BackendType.Kind == LLVMTypeKind.LLVMVectorTypeKind && v.BackendType.ElementType.Kind == elementType && v.BackendType.VectorSize == numElements)
                return true;
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

        enum IntrinsicKind
        {
            Atomic,
            Vector
        }

        static IntrinsicKind DecodeKindFromName(string name, out string Kind, out string subName, out uint numElements)
        {
            Kind = "";
            numElements = 0;
            if (name.StartsWith("Intrinsic_Atomic"))
            {
                subName = name.Substring("Intrinsic_Atomic".Length);
                return IntrinsicKind.Atomic;
            }
            if (name.StartsWith("Intrinsic_Vec"))
            {
                subName = name.Substring("Intrinsic_Vec".Length);
                int len = 0;
                foreach(char c in subName)
                {
                    if (!Char.IsDigit(c))
                        break;
                    len++;
                }
                var num = subName.Substring(0, len);
                numElements = UInt32.Parse(num);
                subName = subName.Substring(len);
                Kind = subName.Substring(0,1);
                subName=subName.Substring(1);
                return IntrinsicKind.Vector;
            }
            throw new System.NotImplementedException($"{name} is not handled in decodekindfromname");
        }

        public static IType ResolveOutputType(SemanticPass pass, AstLoadableIdentifier ident, IType[] inputs, AstFunctionType functionType)
        {
            var kind = DecodeKindFromName(ident.Name, out _, out var name, out _);
            switch (kind)
            {
                case IntrinsicKind.Atomic:
                    return functionType.ResolveOutputType(pass);
                case IntrinsicKind.Vector:
                    switch (name)
                    {
                        case "Dot":
                            return functionType.ResolveOutputType(pass);
                        case "Add":
                        case "Sub":
                        case "Mul":
                        case "Div":
                        case "Floor":
                            return inputs[0];
                    }
                    break;
            }
            throw new NotImplementedException($"Unhandled Builtin '{ident.Name}'");
        }

        public static ICompilationValue CallBuiltIn(CompilationUnit unit, CompilationBuilder builder, CompilationValue function, CompilationFunctionType ftype, CompilationValue[] inputs)
        {
            var kind = DecodeKindFromName(ftype.Identifier, out var vectorKind, out var name, out var numElements);

            switch (kind)
            {
                case IntrinsicKind.Atomic:
                    switch (name)
                    {
                        case "LoadExplicit":
                            return builder.LoadAtomic(ftype.ReturnType.Type, inputs[0], GetAtomicOrdering(inputs[1]));
                        case "StoreExplicit":
                        {
                            builder.StoreAtomic(inputs[1], inputs[0], GetAtomicOrdering(inputs[2]));
                            return inputs[1];
                        }
                    }
                    break;
                case IntrinsicKind.Vector:
                    switch (name)
                    {
                        case "Add":
                        case "Sub":
                        case "Mul":
                        case "Div":
                        case "Dot":
                            {
                                if (vectorKind!="F")
                                {
                                    throw new NotImplementedException($"TODO - Only F type vector supported at present");
                                }
                                // Step 2 validate our inputs are expected
                                if (IsIntrinsicTypeCorrect(inputs[0], numElements, LLVMTypeKind.LLVMFloatTypeKind) && IsIntrinsicTypeCorrect(inputs[1], numElements, LLVMTypeKind.LLVMFloatTypeKind))
                                {
                                    var vecA = builder.StructToVec(inputs[0], numElements);
                                    var vecB = builder.StructToVec(inputs[1], numElements);
                                    LLVMValueRef res;

                                    switch (name)
                                    {
                                        case "Add":
                                            res = builder.FAdd(vecA, vecB);
                                            break;
                                        case "Sub":
                                            res = builder.FSub(vecA, vecB);
                                            break;
                                        case "Mul":
                                            res = builder.FMul(vecA, vecB);
                                            break;
                                        case "Div":
                                            res = builder.FDiv(vecA, vecB);
                                            break;
                                        case "Dot":
                                            res = builder.FDot(vecA, vecB);
                                            break;
                                        default:
                                            throw new NotImplementedException($"Built in function {ftype.Identifier} not implemented");
                                    }

                                    if (res.TypeOf.Kind==LLVMTypeKind.LLVMFloatTypeKind)
                                        return builder.FloatTo(res, (inputs[0].Type as CompilationStructureType).Elements[0], function.FrontendLocation);

                                    return builder.VecToStruct(res, inputs[0].Type, numElements, function.FrontendLocation);
                                }

                                throw new Exception($"Built in function ({ftype.Identifier} types mismatch) - Something is wrong");
                            }
                        case "Floor":    // 2Vec in 1Vec out
                            {
                                // Step 2 validate our inputs are expected
                                if (IsIntrinsicTypeCorrect(inputs[0], numElements, LLVMTypeKind.LLVMFloatTypeKind))
                                {
                                    var vecA = builder.StructToVec(inputs[0], numElements);
                                    LLVMValueRef res;

                                    res = builder.FFloor(vecA);

                                    return builder.VecToStruct(res, inputs[0].Type, numElements, function.FrontendLocation);
                                }

                                throw new Exception($"Built in function (Vec2FFloor types mismatch) - Something is wrong");
                            }
                    }
                    break;
            }

            throw new NotImplementedException($"Built in function {ftype.Identifier} not implemented");
        }

    }
}
