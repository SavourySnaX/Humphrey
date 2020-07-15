using LLVMSharp.Interop;
using System;
using System.Text;

namespace Extensions
{
    public unsafe static class Helpers
    {
        public static LLVMPassManagerBuilderRef PassManagerBuilderCreate()
        {
            return LLVM.PassManagerBuilderCreate();
        }

        public static LLVMContextRef CreateContext()
        {
            return LLVM.ContextCreate();
        }

        public static LLVMValueRef ConstIntToPtr(this LLVMValueRef valueRef, LLVMTypeRef typeRef)
        {
            return LLVM.ConstIntToPtr(valueRef, typeRef);
        }

        public static LLVMValueRef CreateConstantValue(this LLVMTypeRef type, string value, int radix)
        {
            if (radix < 1 || radix > 255)
                throw new ArgumentException($"Radix must be in the range 1-255 radix passed was {radix}");
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException($"Value must be a valid string not null/empty");

            fixed (byte* bvalue = Encoding.ASCII.GetBytes(value))
            {
                return LLVM.ConstIntOfString(type, (sbyte*)bvalue, (byte)radix);
            }
        }

        public static LLVMTypeRef CreateFunctionType(LLVMTypeRef returnType, LLVMTypeRef[] paramTypes, bool isVarArg)
        {
            uint numParams = (uint)paramTypes.Length;
            var opaque = new LLVMOpaqueType*[numParams];
            for (int a = 0; a < paramTypes.Length; a++)
                opaque[a] = paramTypes[a];
            fixed (LLVMOpaqueType** types = opaque)
            {
                return LLVM.FunctionType(returnType, types, numParams, isVarArg ? 1 : 0);
            }
        }

        public static LLVMTypeRef CreatePointerType(LLVMTypeRef elementType, uint addressSpace = 0)
        {
            return LLVM.PointerType(elementType, addressSpace);
        }

        public static LLVMTypeRef CreateArrayType(LLVMTypeRef elementType, uint numElements)
        {
            return LLVM.ArrayType(elementType, numElements);
        }
    }
}
