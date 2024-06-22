using System;
using System.IO;
using System.Text;
using LLVMSharp.Interop;
using static Extensions.Helpers;

namespace Humphrey.Backend
{
    public class CompilationDebugBuilder
    {
        CompilationUnit unit;
        LLVMDIBuilderRef builderRef;
        LLVMMetadataRef debugCU;
        LLVMMetadataRef debugScope;
        bool optimised;
        bool enabled;

        public enum BasicType
        {
            Float,
            SignedInt,
            UnsignedInt
        }

        public CompilationDebugBuilder(bool enable, CompilationUnit cu, string fileNameAndPath, string compilerVersion, bool codeView)
        {
            enabled = enable;
            if (enabled)
            {
                string flags = "";
                string splitName = "";
                uint runtimeVersion = 0;
                uint dwOld = 0;
                int splitDebugInlining = 1;
                int debugInfoForProfiling = 1;

                unit = cu;
                optimised = false;
                builderRef = CreateDIBuilder(unit.Module);

                unit.AddModuleFlag(LLVMModuleFlagBehavior.LLVMModuleFlagBehaviorWarning, "Debug Info Version", GetDebugMetaVersion());

                if (codeView)
                {
                    unit.AddModuleFlag(LLVMModuleFlagBehavior.LLVMModuleFlagBehaviorWarning, "CodeView", 1);
                }
                else
                {
                    unit.AddModuleFlag(LLVMModuleFlagBehavior.LLVMModuleFlagBehaviorWarning, "Dwarf Version", 2);
                }

                debugScope = CreateDebugFile(fileNameAndPath);
                debugCU = builderRef.CreateCompileUnit(LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageC,
                    debugScope,
                    compilerVersion, optimised ? 1 : 0, flags, runtimeVersion, splitName, LLVMDWARFEmissionKind.LLVMDWARFEmissionFull,
                    dwOld, splitDebugInlining, debugInfoForProfiling, "", "");
            }
        }

        private LLVMMetadataRef CreateDebugFile(string fileNameAndPath)
        {
            if (enabled)
            {
                if (string.IsNullOrEmpty(fileNameAndPath))
                    return builderRef.CreateFile("empty", "empty");
                return builderRef.CreateFile(Path.GetFileName(fileNameAndPath), Path.GetDirectoryName(Path.GetFullPath(fileNameAndPath)));
            }
            return default;
        }

        public string AsciiSafeName(string nameToMangle)
        {
            if (enabled)
            {
                return Encoding.ASCII.GetString(
                    Encoding.Convert(Encoding.UTF8, Encoding.GetEncoding(
                        Encoding.ASCII.EncodingName,
                        new EncoderReplacementFallback("_"),
                        new DecoderExceptionFallback()
                    ), Encoding.UTF8.GetBytes(nameToMangle)));
            }
            return default;
        }

        public CompilationDebugType CreateFunctionType(string name, CompilationDebugType returnType, CompilationDebugType[] parameterTypes, SourceLocation location)
        {
            if (enabled)
            {
                var flags = LLVMDIFlags.LLVMDIFlagPublic;
                var paramTypes = new LLVMMetadataRef[parameterTypes.Length + 1]; // return type is first
                var idx = 0;
                paramTypes[idx++] = returnType == null ? null : returnType.BackendType;   // for now all our functions are void return types
                foreach (var t in parameterTypes)
                {
                    paramTypes[idx++] = t.BackendType;
                }
                return new CompilationDebugType(name, builderRef.CreateSubroutineType(CreateDebugFile(location.File), paramTypes, flags));
            }
            return default;
        }

        public LLVMMetadataRef CreateDebugFunction(string functionName, SourceLocation location, CompilationFunctionType functionType)
        {
            if (enabled)
            {
                var localToUnit = 0;
                var definition = 1;
                var scopeLine = location.StartLine;
                var flags = LLVMDIFlags.LLVMDIFlagPublic;
                var isOptimised = optimised ? 1 : 0;

                return builderRef.CreateFunction(debugScope, functionName, AsciiSafeName(functionName),
                    CreateDebugFile(location.File), location.StartLine, functionType.DebugType.BackendType,
                    localToUnit, definition, scopeLine, flags, isOptimised);
            }
            return default;
        }

        public LLVMMetadataRef CreateLexicalScope(LLVMMetadataRef parentScope, SourceLocation location)
        {
            if (enabled)
            {
                return builderRef.CreateLexicalBlock(parentScope, CreateDebugFile(location.File), location.StartLine, location.StartColumn);
            }
            return default;
        }

        public CompilationDebugType CreateBasicType(string name, System.UInt64 numBits, BasicType type)
        {
            if (enabled)
            {
                var dwarfType = LLVMDwarfATEValues.None;
                switch (type)
                {
                    case BasicType.Float:
                        dwarfType = LLVMDwarfATEValues.DW_ATE_float;
                        break;
                    case BasicType.SignedInt:
                        dwarfType = LLVMDwarfATEValues.DW_ATE_signed;
                        break;
                    case BasicType.UnsignedInt:
                        dwarfType = LLVMDwarfATEValues.DW_ATE_unsigned;
                        break;
                    default:
                        throw new System.NotImplementedException($"Unhandled Basic Type in CreateBasicType {type}");
                }
                return new CompilationDebugType(name, builderRef.CreateBasicType(name, numBits, dwarfType));
            }
            return default;
        }

        public CompilationDebugType CreatePointerType(string name, CompilationDebugType element)
        {
            if (enabled)
            {
                uint ptrAlign = 0;
                uint ptrAddressSpace = 0;
                var dbgType = builderRef.CreatePointerType(element.BackendType, unit.GetPointerSizeInBits(), ptrAlign, ptrAddressSpace, name);
                return new CompilationDebugType(name, dbgType);
            }
            return default;
        }

        public LLVMMetadataRef CreateParameterVariable(string name, LLVMMetadataRef parentScope, SourceLocation location, uint argNo, LLVMMetadataRef type)
        {
            if (enabled)
            {
                return builderRef.CreateParameterVariable(parentScope, name, argNo, CreateDebugFile(location.File), location.StartLine, type);
            }
            return default;
        }

        public void InsertDeclareAtEnd(CompilationValue storage, LLVMMetadataRef varInfo, SourceLocation location, CompilationBlock block)
        {
            if (enabled)
            {
                var debugLog = unit.CreateDebugLocationMeta(location);
                builderRef.InsertDeclareAtEnd(storage.BackendValue, varInfo, builderRef.CreateEmptyExpression(), debugLog, block.BackendValue);
            }
        }

        public LLVMMetadataRef CreateAutoVariable(string name, LLVMMetadataRef parentScope, SourceLocation location, CompilationDebugType type)
        {
            if (enabled)
            {
                var preserveAlways = true;
                var alignBits = 0u;
                var flags = LLVMDIFlags.LLVMDIFlagPublic;

                return builderRef.CreateAutoVariable(parentScope, name, CreateDebugFile(location.File), location.StartLine, type.BackendType, preserveAlways, flags, alignBits);
            }
            return default;
        }

        public LLVMMetadataRef CreateGlobalVarable(string name, LLVMMetadataRef scope, SourceLocation location, CompilationDebugType type)
        {
            if (enabled)
            {
                var isVisibleExternally = true;
                LLVMMetadataRef expr = builderRef.CreateEmptyExpression();
                LLVMMetadataRef decl = null;
                var alignBits = 0u;
                return builderRef.CreateGlobalVariable(scope, name, AsciiSafeName(name), CreateDebugFile(location.File), location.StartLine, type.BackendType, isVisibleExternally, expr, decl, alignBits);
            }
            return default;
        }


        public LLVMMetadataRef RootScope => enabled ? debugScope : default;

        public struct StructLayout 
        {
            public StructLayout(CompilationUnit unit, CompilationStructureType structureType)
            {
                structSize = 0;
                elementOffsets = new UInt64[structureType.Elements.Length];
                elementSizeInStruct = new ulong[structureType.Elements.Length];
                // Only handles packed structs!
                var dataLayout = unit.Module.GetDataLayout();
                var numElements = structureType.Elements.Length;
                for (int idx = 0; idx < numElements; idx++)
                {
                    var type = structureType.Elements[idx].BackendType;
                    var abiSize = dataLayout.GetABISizeOfType(type);
                    elementOffsets[idx] = structSize;
                    elementSizeInStruct[idx] = abiSize;
                    structSize += abiSize;
                }
            }

            public uint GetElementOffsetInBits(int idx)
            {
                if (idx<0 || idx>=elementOffsets.Length)
                    throw new ArgumentException($"Argument idx {idx} out of range");
                if (elementOffsets[idx]*8 > uint.MaxValue)
                    throw new ArgumentException($"elementOffset*8 > uint size {elementOffsets[idx]}");
                return (uint)(elementOffsets[idx] * 8);
            }

            public UInt64 GetElementSizeInBits(int idx)
            {
                if (idx<0 || idx>=elementOffsets.Length)
                    throw new ArgumentException($"Argument idx {idx} out of range");
                return elementSizeInStruct[idx] * 8;
            }

            public UInt64 StructSizeInBits()
            {
                return structSize * 8;
            }

            UInt64 structSize;
            UInt64[] elementSizeInStruct;
            UInt64[] elementOffsets;
        }

        public StructLayout GetStructLayout(CompilationStructureType structType)
        {
            if (enabled)
            {
                return new StructLayout(unit, structType);
            }
            return default;
        }

        public CompilationDebugType CreateForwardStructureType(string name, CompilationStructureType structType)
        {
            if (enabled)
            {
                var structureType = builderRef.CreateForwardStruct(debugScope, name, CreateDebugFile(structType.Location.File), structType.Location.StartLine);
                return new CompilationDebugType(name, structureType);
            }
            return default;
        }

        public void ReplaceForwardStructWithFinal(CompilationDebugType frwd, CompilationDebugType final)
        {
            if (enabled)
            {
                builderRef.ReplaceForwardStructWithFinal(frwd.BackendType, final.BackendType);
            }
        }

        public CompilationDebugType CreateAliasType(string name, CompilationAliasType aliasType)
        {
            if (enabled)
            {
                int totalElements=1;
                for (int unionIdx=0;unionIdx<aliasType.Elements.Length;unionIdx++)
                {
                    for (int structIdx=0;structIdx<aliasType.ElementNames[unionIdx].Length;structIdx++)
                    {
                        if (aliasType.ElementNames[unionIdx][structIdx]!="_")
                        {
                            totalElements++;
                        }
                    }
                }

                var dataLayout = unit.Module.GetDataLayout();
                var unionElements = new LLVMMetadataRef[totalElements];
                var elementIdx=0;
                // First element is hardcoded (raw)
                {
                        var location = aliasType.Location;
                        var type = aliasType.BaseType.DebugType.BackendType;
                        var sizeInBits=dataLayout.GetTypeSizeInBits(aliasType.BaseType.BackendType);
                        uint offsetBits=0;
                        uint alignBits=1;
                        var flags=LLVMDIFlags.LLVMDIFlagBitField;
                        unionElements[elementIdx++] = builderRef.CreateMemberType(debugScope,"raw",CreateDebugFile(location.File), location.StartLine, sizeInBits, alignBits, offsetBits, flags, type);
                }
                // Other elements are added as is, so long as they are not named _ (ordered MSB->LSB)
                for (int unionIdx=0;unionIdx<aliasType.Elements.Length;unionIdx++)
                {
                    var structMemberOffsetBits=dataLayout.GetTypeSizeInBits(aliasType.BaseType.BackendType);
                    for (int structIdx=0;structIdx<aliasType.Elements[unionIdx].Length;structIdx++)
                    {
                        var sizeInBits=dataLayout.GetTypeSizeInBits(aliasType.Elements[unionIdx][structIdx].BackendType);
                        structMemberOffsetBits-=sizeInBits;
                        if (aliasType.ElementNames[unionIdx][structIdx]!="_")
                        {
                            var location = aliasType.Elements[unionIdx][structIdx].Location;
                            var type = aliasType.BaseType.DebugType.BackendType;
                            uint offsetBits=(uint)structMemberOffsetBits;
                            uint alignBits=1;
                            var flags=LLVMDIFlags.LLVMDIFlagBitField;
                            unionElements[elementIdx++] = builderRef.CreateMemberType(debugScope,aliasType.ElementNames[unionIdx][structIdx],CreateDebugFile(location.File), location.StartLine, sizeInBits, alignBits, offsetBits, flags, type);
                        }
                    }
                }
                var uSizeBits = dataLayout.GetTypeSizeInBits(aliasType.BaseType.BackendType);
                var uAlignBits = 8u;
                var uFlags = LLVMDIFlags.LLVMDIFlagPublic;
                var unionType = builderRef.CreateUnion(debugScope, name, CreateDebugFile(aliasType.Location.File), aliasType.Location.StartLine, uSizeBits, uAlignBits, uFlags, unionElements);
                return new CompilationDebugType(name, unionType);
            }
            return default;
        }

        public CompilationDebugType CreateStructureType(string name, CompilationStructureType structType)
        {
            if (enabled)
            {
                var dataLayout = unit.Module.GetDataLayout();
                var structLayout = GetStructLayout(structType);
                var structElements = new LLVMMetadataRef[structType.Elements.Length];
                var alignBits = 8u;  // structures are always packed at present
                var flags = LLVMDIFlags.LLVMDIFlagPublic;

                for (int idx = 0; idx < structType.Elements.Length; idx++)
                {
                    var offsetBits = structLayout.GetElementOffsetInBits(idx);
                    var sizeBits = structLayout.GetElementSizeInBits(idx);
                    var location = structType.Elements[idx].Location;
                    var dbgType = builderRef.CreateMemberType(debugScope, structType.Fields[idx], CreateDebugFile(location.File), location.StartLine, sizeBits, alignBits, offsetBits, flags, structType.Elements[idx].DebugType.BackendType);
                    structElements[idx] = dbgType;
                }

                var structSizeBits = structLayout.StructSizeInBits();
                var structureType = builderRef.CreateStruct(debugScope, name, CreateDebugFile(structType.Location.File), structType.Location.StartLine, structSizeBits, alignBits, flags, structElements);
                return new CompilationDebugType(name, structureType);
            }
            return default;
        }

        public CompilationDebugType CreateArrayType(string name, CompilationArrayType arrayType)
        {
            if (enabled)
            {
                var dataLayout = unit.Module.GetDataLayout();
                var subscripts = new LLVMMetadataRef[1];
                subscripts[0] = builderRef.GetOrCreateSubrange(0, arrayType.ElementCount);
                var sizeBits = dataLayout.GetTypeSizeInBits(arrayType.BackendType);
                var alignBits = 8u;
                var dbgType = builderRef.CreateArray(sizeBits, alignBits, arrayType.ElementType.DebugType.BackendType, subscripts);
                return new CompilationDebugType(name, dbgType);
            }
            return default;
        }

        public CompilationDebugType CreateEnumType(string name, CompilationEnumType enumType)
        {
            if (enabled)
            {
                var dataLayout = unit.Module.GetDataLayout();
                var elements = new LLVMMetadataRef[enumType.Elements.Length];
                if (enumType.ElementType is CompilationIntegerType et)
                {
                    var isSigned = et.IsSigned;
                    for (int idx = 0; idx < elements.Length; idx++)
                    {
                        var valName = enumType.Elements[idx];
                        var value = enumType.GetElementValue(valName);

                        elements[idx] = builderRef.CreateEnumerator(valName, value, isSigned);
                    }
                }
                else
                    throw new NotImplementedException($"Debug information for enumerations requires integer types");

                var sizeBits = dataLayout.GetTypeSizeInBits(enumType.ElementType.BackendType);
                var alignBits = dataLayout.GetABIAlignmentOfType(enumType.ElementType.BackendType) * 8;
                var dbgType = builderRef.CreateEnum(debugScope, name, CreateDebugFile(enumType.Location.File), enumType.Location.StartLine, sizeBits, alignBits, elements, enumType.ElementType.DebugType.BackendType);
                return new CompilationDebugType(name, dbgType);
            }
            return default;
        }

        public bool Enabled => enabled;

        public void Finalise()
        {
            if (enabled)
            {
                builderRef.DIBuilderFinalize();
            }
        }

    }
}