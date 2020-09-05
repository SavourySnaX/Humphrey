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
        public static LLVMValueRef CreateConstantValue(this LLVMTypeRef type, UInt64 value)
        {
            return CreateConstantValue(type, value.ToString(), 10);
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

        public static void ParseCommandLineOptions(string[] options, string overview)
        {
            int argc = options.Length;
            byte[][] toManaged = new byte[argc][];
            int a = 0;
            var onstackArray = stackalloc sbyte*[argc];
            foreach (var opt in options)
            {
                var bArray = Encoding.ASCII.GetBytes(options[a]);
                var bAlloc = stackalloc sbyte[bArray.Length + 1];
                for (int b = 0; b < bArray.Length;b++)
                {
                    bAlloc[b] = (sbyte)bArray[b];
                }
                onstackArray[a++] = bAlloc;
            }

            fixed (byte* pOverview = Encoding.ASCII.GetBytes(overview))
            {
                LLVM.ParseCommandLineOptions(argc, onstackArray, (sbyte*)pOverview);
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
        public static LLVMTypeRef CreateIntType(uint numBits)
        {
            return LLVM.IntType(numBits);
        }

        public static LLVMValueRef FetchIntrinsic(LLVMModuleRef moduleRef, string intrinsicName, LLVMTypeRef[] paramTypes)
        {
            if (string.IsNullOrEmpty(intrinsicName))
                throw new ArgumentException($"Value must be a valid string not null/empty");

            uint ID;
            fixed (byte* bvalue = Encoding.ASCII.GetBytes(intrinsicName))
            {
                ID = LLVM.LookupIntrinsicID((sbyte*)bvalue, (UIntPtr)intrinsicName.Length);
            }
            uint numParams = (uint)paramTypes.Length;
            var opaque = new LLVMOpaqueType*[numParams];
            for (int a = 0; a < paramTypes.Length; a++)
                opaque[a] = paramTypes[a];

            fixed (LLVMOpaqueType** types = opaque)
            {
                return LLVM.GetIntrinsicDeclaration(moduleRef, ID, types, (UIntPtr)numParams);
            }
        }

        public static LLVMDIBuilderRef CreateDIBuilder(LLVMModuleRef moduleRef)
        {
            return LLVM.CreateDIBuilder(moduleRef);
        }

        public static void AddModuleFlag(this LLVMModuleRef moduleRef, LLVMModuleFlagBehavior flagBehavior, string flagName, LLVMMetadataRef flagValue)
        {
            fixed (byte* flagNamePtr = Encoding.ASCII.GetBytes(flagName))
            {
                LLVM.AddModuleFlag(moduleRef, flagBehavior, (sbyte*)flagNamePtr, (UIntPtr)flagName.Length, flagValue);
            }
        }

        public static LLVMMetadataRef AsMetadata(this LLVMValueRef valueRef)
        {
            return LLVM.ValueAsMetadata(valueRef);
        }

        public static LLVMValueRef MDString(this LLVMContextRef contextRef, string value)
        {
            fixed (byte* valuePtr = Encoding.ASCII.GetBytes(value))
            {
                return LLVM.MDStringInContext(contextRef, (sbyte*)valuePtr, (uint)value.Length);
            }
        }

        public static void AddNamedMetadataWithStringValue(this LLVMModuleRef moduleRef, LLVMContextRef contextRef, string key, string value)
        {
            var mdString = MDString(contextRef, value);
            fixed (byte* keyPtr = Encoding.ASCII.GetBytes(key))
            {
                LLVM.AddNamedMetadataOperand(moduleRef, (sbyte*)keyPtr, mdString);
            }
        }

        public static uint GetDebugMetaVersion()
        {
            return LLVM.DebugMetadataVersion();
        }
    }
}
