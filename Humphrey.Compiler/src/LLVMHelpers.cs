using LLVMSharp.Interop;
using System;
using System.Text;

namespace Extensions
{
    public unsafe static class Helpers
    {
        public static string GetDefaultTargetTriple()
        {
            return LLVMTargetRef.DefaultTriple;
        }

        public static LLVMPassManagerBuilderRef PassManagerBuilderCreate()
        {
            return LLVM.PassManagerBuilderCreate();
        }

        public static LLVMContextRef CreateContext()
        {
            return LLVM.ContextCreate();
        }

        public static LLVMContextRef FetchGlobalContext()
        {
            return LLVM.GetGlobalContext();
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

        public static LLVMValueRef CreateConstantArrayFromValues(LLVMValueRef[] constants, LLVMTypeRef type)
        {
            var toManaged = new LLVMOpaqueValue*[constants.Length];
            int a=0;
            foreach (var value in constants)
            {
                toManaged[a++]=value;
            }
            fixed (LLVMOpaqueValue** array = toManaged)
            {
                return LLVM.ConstArray(type, array, (uint)toManaged.Length);
            }
        }

        public static void ParseCommandLineOptions(string[] options, string overview)
        {
            int argc = options.Length;
            int a = 0;
            var onstackArray = stackalloc sbyte*[argc];
            int totLength = 0;
            foreach (var opt in options)
            {
                totLength += Encoding.ASCII.GetByteCount(options[a]) + 1;
            }
            var bAlloc = stackalloc sbyte[totLength];
            int offset = 0;
            foreach (var opt in options)
            {
                var bArray = Encoding.ASCII.GetBytes(options[a]);
                for (int b = 0; b < bArray.Length;b++)
                {
                    bAlloc[offset+b] = (sbyte)bArray[b];
                }
                onstackArray[a++] = &bAlloc[offset];
                offset += bArray.Length + 1;
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
        public static LLVMTypeRef CreateIntType(LLVMContextRef context, uint numBits)
        {
            return LLVM.IntTypeInContext(context, numBits);
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

        public static LLVMTypeRef FetchIntrinsicFunctionType(LLVMModuleRef moduleRef, string intrinsicName, LLVMTypeRef[] paramTypes)
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
                return LLVM.IntrinsicGetType(moduleRef.Context, ID, types, (UIntPtr)numParams);
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
        public static LLVMValueRef AsValue(this LLVMMetadataRef metadataRef, LLVMContextRef contextRef)
        {
            return LLVM.MetadataAsValue(contextRef, metadataRef);
        }

        public enum LLVMDwarfATEValues : uint
        {
            None = 0x00,
            DW_ATE_address = 0x01,
            DW_ATE_boolean = 0x02,
            DW_ATE_complex_float = 0x03,
            DW_ATE_float = 0x04,
            DW_ATE_signed = 0x05,
            DW_ATE_signed_char = 0x06,
            DW_ATE_unsigned = 0x07,
            DW_ATE_unsigned_char = 0x08,
            DW_ATE_imaginary_float = 0x09,
            DW_ATE_packed_decimal = 0x0a,
            DW_ATE_numeric_string = 0x0b,
            DW_ATE_edited = 0x0c,
            DW_ATE_signed_fixed = 0x0d,
            DW_ATE_unsigned_fixed = 0x0e,
            DW_ATE_decimal_float = 0x0f,
        };

        public enum LLVMDwarfTag : uint
        {
            DW_TAG_structure_type = 0x0013,
        }

        public static LLVMMetadataRef CreateBasicType(this LLVMDIBuilderRef builderRef, string name, UInt64 numBits, LLVMDwarfATEValues type)
        {
            fixed (byte* namePtr = Encoding.ASCII.GetBytes(name))
            {
                return LLVM.DIBuilderCreateBasicType(builderRef, (sbyte*)namePtr, (UIntPtr)name.Length, numBits, (uint)type, LLVMDIFlags.LLVMDIFlagPublic);
            }
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

        public static LLVMMetadataRef GetOrCreateTypeArray(this LLVMDIBuilderRef builderRef, LLVMMetadataRef[] types)
        {
            uint numTypes = (uint)types.Length;
            var opaque = new LLVMOpaqueMetadata*[numTypes];
            for (int a = 0; a < types.Length; a++)
                opaque[a] = types[a];

            fixed (LLVMOpaqueMetadata** opaqueTypes = opaque)
            {
                return LLVM.DIBuilderGetOrCreateTypeArray(builderRef, opaqueTypes, (UIntPtr)numTypes);
            }
        }

        public static void SetSubprogram(this LLVMValueRef function, LLVMMetadataRef debugFunction)
        {
            LLVM.SetSubprogram(function, debugFunction);
        }

        public static LLVMMetadataRef GetSubprogram(this LLVMValueRef function)
        {
            return LLVM.GetSubprogram(function);
        }

        public static LLVMMetadataRef CreateLexicalBlock(this LLVMDIBuilderRef builderRef, LLVMMetadataRef parentScope, LLVMMetadataRef file, uint line, uint column)
        {
            return LLVM.DIBuilderCreateLexicalBlock(builderRef, parentScope, file, line, column);
        }

        public static LLVMMetadataRef CreateParameterVariable(this LLVMDIBuilderRef builderRef, LLVMMetadataRef parentScope, string name, uint argNumber, LLVMMetadataRef file, uint lineNo, LLVMMetadataRef type)
        {
            var preserve = 1;
            var flags = LLVMDIFlags.LLVMDIFlagPublic;

            fixed (byte* namePtr = Encoding.ASCII.GetBytes(name))
            {
                return LLVM.DIBuilderCreateParameterVariable(builderRef, parentScope, (sbyte*)namePtr, (UIntPtr)name.Length, argNumber, file, lineNo, type, preserve, flags);
            }
        }

        public static LLVMMetadataRef CreateEmptyExpression(this LLVMDIBuilderRef builderRef)
        {
            return LLVM.DIBuilderCreateExpression(builderRef, null, (UIntPtr)0);
        }

        public static LLVMValueRef InsertDeclareAtEnd(this LLVMDIBuilderRef builderRef, LLVMValueRef storage, LLVMMetadataRef varInfo, LLVMMetadataRef expr, LLVMMetadataRef debugLoc, LLVMBasicBlockRef atEnd)
        {
            return LLVM.DIBuilderInsertDeclareAtEnd(builderRef, storage, varInfo, expr, debugLoc, atEnd);
        }

        public static LLVMMetadataRef CreatePointerType(this LLVMDIBuilderRef builderRef, LLVMMetadataRef pointee, UInt64 sizeInBits, uint alignInBits, uint addressSpace, string name)
        {
            fixed (byte* namePtr = Encoding.ASCII.GetBytes(name))
            {
                return LLVM.DIBuilderCreatePointerType(builderRef, pointee, sizeInBits, alignInBits, addressSpace, (sbyte*)namePtr, (UIntPtr)name.Length);
            }
        }

        public static void SetDataLayout(this LLVMModuleRef moduleRef, LLVMTargetDataRef dataLayout)
        {
            LLVM.SetModuleDataLayout(moduleRef, dataLayout);
        }

        public static LLVMTargetDataRef GetDataLayout(this LLVMModuleRef moduleRef)
        {
            return LLVM.GetModuleDataLayout(moduleRef);
        }

        public static UInt64 GetTypeSizeInBits(this LLVMTargetDataRef targetDataRef, LLVMTypeRef type)
        {
            return LLVM.SizeOfTypeInBits(targetDataRef, type);
        }

        public static uint GetABIAlignmentOfType(this LLVMTargetDataRef targetDataRef, LLVMTypeRef type)
        {
            return LLVM.ABIAlignmentOfType(targetDataRef, type);
        }

        public static UInt64 GetPointerSizeInBits(this LLVMTargetDataRef targetDataRef)
        {
            return LLVM.PointerSize(targetDataRef) * 8;
        }

        public static UInt64 GetABISizeOfType(this LLVMTargetDataRef targetDataRef, LLVMTypeRef typeRef)
        {
            return LLVM.ABISizeOfType(targetDataRef, typeRef);
        }

        public static LLVMValueRef GetConstNull(this LLVMTypeRef typeRef)
        {
            return LLVM.ConstNull(typeRef);
        }

        public static LLVMMetadataRef CreateMemberType(this LLVMDIBuilderRef debugBuilder, LLVMMetadataRef scope, string name, LLVMMetadataRef file, uint line, UInt64 bitSize, uint alignBits, uint offsetBits, LLVMDIFlags flags, LLVMMetadataRef type)       
        {
            fixed (byte* namePtr = Encoding.ASCII.GetBytes(name))
            {
                return LLVM.DIBuilderCreateMemberType(debugBuilder, scope, (sbyte*)namePtr, (UIntPtr)name.Length, file, line, bitSize, alignBits, offsetBits, flags, type);
            }
        }

        public static LLVMMetadataRef CreateForwardStruct(this LLVMDIBuilderRef debugBuilder, LLVMMetadataRef scope, string name, LLVMMetadataRef file, uint line)
        {
            uint runtimeLang = 0;
            string unique = "";
            fixed (byte* namePtr = Encoding.ASCII.GetBytes(name))
            {
                fixed (byte* uniquePtr = Encoding.ASCII.GetBytes(unique))
                {
                    return LLVM.DIBuilderCreateReplaceableCompositeType(debugBuilder, (uint)LLVMDwarfTag.DW_TAG_structure_type, (sbyte*)namePtr, (UIntPtr)name.Length, scope, file, line, runtimeLang, 0, 0, LLVMDIFlags.LLVMDIFlagFwdDecl, (sbyte*)uniquePtr, (UIntPtr)unique.Length);
                }
            }
        }

        public static void ReplaceForwardStructWithFinal(this LLVMDIBuilderRef debugBuilder, LLVMMetadataRef frwd, LLVMMetadataRef final)
        {
            LLVM.MetadataReplaceAllUsesWith(frwd,final);
        }

        public static LLVMMetadataRef CreateUnion(this LLVMDIBuilderRef debugBuilder, LLVMMetadataRef scope, string name, LLVMMetadataRef file, uint line, UInt64 sizeBits, uint alignBits, LLVMDIFlags flags, LLVMMetadataRef[] elements)
        {
            uint runtimeLang = 0;
            LLVMMetadataRef derivedFrom = null;
            LLVMMetadataRef vtableHolder = null;
            string unique = "";
            fixed (byte* namePtr = Encoding.ASCII.GetBytes(name))
            {
                fixed (byte* uniquePtr = Encoding.ASCII.GetBytes(unique))
                {
                    uint numTypes = (uint)elements.Length;
                    var opaque = new LLVMOpaqueMetadata*[numTypes];
                    for (int a = 0; a < elements.Length; a++)
                        opaque[a] = elements[a];

                    fixed (LLVMOpaqueMetadata** opaqueTypes = opaque)
                    {
                        return LLVM.DIBuilderCreateUnionType(debugBuilder, scope, (sbyte*)namePtr, (UIntPtr)name.Length,file,line,sizeBits,alignBits,flags,opaqueTypes,numTypes,runtimeLang,(sbyte*)uniquePtr,(UIntPtr)unique.Length);
                    }
                }
            }
        }

        public static LLVMMetadataRef CreateStruct(this LLVMDIBuilderRef debugBuilder, LLVMMetadataRef scope, string name, LLVMMetadataRef file, uint line, UInt64 sizeBits, uint alignBits, LLVMDIFlags flags, LLVMMetadataRef[] elements)
        {
            uint runtimeLang = 0;
            LLVMMetadataRef derivedFrom = null;
            LLVMMetadataRef vtableHolder = null;
            string unique = "";
            fixed (byte* namePtr = Encoding.ASCII.GetBytes(name))
            {
                fixed (byte* uniquePtr = Encoding.ASCII.GetBytes(unique))
                {
                    uint numTypes = (uint)elements.Length;
                    var opaque = new LLVMOpaqueMetadata*[numTypes];
                    for (int a = 0; a < elements.Length; a++)
                        opaque[a] = elements[a];

                    fixed (LLVMOpaqueMetadata** opaqueTypes = opaque)
                    {
                        return LLVM.DIBuilderCreateStructType(debugBuilder, scope, (sbyte*)namePtr, (UIntPtr)name.Length, file, line, sizeBits, alignBits, flags, derivedFrom, opaqueTypes, numTypes, runtimeLang, vtableHolder, (sbyte*)uniquePtr, (UIntPtr)unique.Length);
                    }
                }
            }
        }

        public static LLVMMetadataRef CreateArray(this LLVMDIBuilderRef debugBuilder, UInt64 sizeBits, uint alignBits, LLVMMetadataRef type, LLVMMetadataRef[] subscripts)
        {
            uint numSubscripts = (uint)subscripts.Length;
            var opaque = new LLVMOpaqueMetadata*[numSubscripts];
            for (int a = 0; a < subscripts.Length; a++)
                opaque[a] = subscripts[a];

            fixed (LLVMOpaqueMetadata** opaqueSubscripts = opaque)
            {
                return LLVM.DIBuilderCreateArrayType(debugBuilder, sizeBits, alignBits, type, opaqueSubscripts, numSubscripts);
            }
        }

        public static LLVMMetadataRef GetOrCreateSubrange(this LLVMDIBuilderRef debugBuilder, Int64 lowerBound, Int64 count)
        {
            return LLVM.DIBuilderGetOrCreateSubrange(debugBuilder, lowerBound, count);
        }

        public static LLVMMetadataRef CreateEnumerator(this LLVMDIBuilderRef debugBuilder, string name, Int64 value, bool signed)
        {
            var isUnsigned = signed ? 0 : 1;
            fixed (byte* namePtr = Encoding.ASCII.GetBytes(name))
            {
                return LLVM.DIBuilderCreateEnumerator(debugBuilder, (sbyte*)namePtr, (UIntPtr)name.Length, value, isUnsigned);
            }
        }

        public static LLVMMetadataRef CreateEnum(this LLVMDIBuilderRef debugBuilder, LLVMMetadataRef scope, string name, LLVMMetadataRef file, uint line, UInt64 sizeBits, uint alignBits, LLVMMetadataRef[] enumerations, LLVMMetadataRef classType)
        {
            fixed (byte* namePtr = Encoding.ASCII.GetBytes(name))
            {
                uint numEnumerations = (uint)enumerations.Length;
                var opaque = new LLVMOpaqueMetadata*[numEnumerations];
                for (int a = 0; a < enumerations.Length; a++)
                    opaque[a] = enumerations[a];

                fixed (LLVMOpaqueMetadata** opaqueEnumerations = opaque)
                {
                    return LLVM.DIBuilderCreateEnumerationType(debugBuilder, scope, (sbyte*)namePtr, (UIntPtr)name.Length, file, line, sizeBits, alignBits, opaqueEnumerations, numEnumerations, classType);
                }
            }
        }

        public static LLVMMetadataRef CreateAutoVariable(this LLVMDIBuilderRef debugBuilder, LLVMMetadataRef scope, string name, LLVMMetadataRef file, uint line, LLVMMetadataRef type, bool preserveAlways, LLVMDIFlags flags, uint alignBits)
        {
            int preserve = preserveAlways ? 1 : 0;
            fixed (byte* namePtr = Encoding.ASCII.GetBytes(name))
            {
                return LLVM.DIBuilderCreateAutoVariable(debugBuilder, scope, (sbyte*)namePtr, (UIntPtr)name.Length, file, line, type, preserve, flags, alignBits);
            }
        }

        public static LLVMMetadataRef CreateGlobalVariable(this LLVMDIBuilderRef debugBuilder, LLVMMetadataRef scope, string name, string linkName, LLVMMetadataRef file, uint line, LLVMMetadataRef type, bool isVisibleExternally, LLVMMetadataRef expr, LLVMMetadataRef decl, uint alignBits)
        {
            int visible = isVisibleExternally ? 1 : 0;
            fixed (byte* namePtr = Encoding.ASCII.GetBytes(name))
            {
                fixed (byte* linkNamePtr = Encoding.ASCII.GetBytes(linkName))
                {
                    return LLVM.DIBuilderCreateGlobalVariableExpression(debugBuilder, scope, (sbyte*)namePtr, (UIntPtr)name.Length, (sbyte*)linkNamePtr, (UIntPtr)linkName.Length, file, line, type, visible, expr, decl, alignBits);
                }
            }
        }

        public static void SetGlobalMetadata(this LLVMValueRef global, LLVMMetadataKind kind, LLVMMetadataRef metadataRef)
        {
            LLVM.GlobalSetMetadata(global, (uint)kind, metadataRef);
        }

        public static uint GetDebugMetaVersion()
        {
            return LLVM.DebugMetadataVersion();
        }
    }
}
