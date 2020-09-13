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

        public enum BasicType
        {
            SignedInt,
            UnsignedInt
        }

        public CompilationDebugBuilder(CompilationUnit cu, string fileNameAndPath, string compilerVersion, bool codeView)
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

            //Finally we need to tag the llvm.ident
            unit.AddNamedMetadata("llvm.ident", compilerVersion);

            debugScope = CreateDebugFile(fileNameAndPath);
            debugCU = builderRef.CreateCompileUnit(LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageC,
                debugScope,
                compilerVersion, optimised ? 1 : 0, flags, runtimeVersion, splitName, LLVMDWARFEmissionKind.LLVMDWARFEmissionFull,
                dwOld, splitDebugInlining, debugInfoForProfiling);
        }

        private LLVMMetadataRef CreateDebugFile(string fileNameAndPath)
        {
            if (string.IsNullOrEmpty(fileNameAndPath))
                return builderRef.CreateFile("empty", "empty");
            return builderRef.CreateFile(Path.GetFileName(fileNameAndPath), Path.GetDirectoryName(Path.GetFullPath(fileNameAndPath)));
        }

        public string AsciiSafeName(string nameToMangle)
        {
            return Encoding.ASCII.GetString(
                Encoding.Convert(Encoding.UTF8, Encoding.GetEncoding(
                    Encoding.ASCII.EncodingName,
                    new EncoderReplacementFallback("_"),
                    new DecoderExceptionFallback()
                ), Encoding.UTF8.GetBytes(nameToMangle)));
        }

        public CompilationDebugType CreateFunctionType(string name, CompilationDebugType[] parameterTypes, SourceLocation location)
        {
            var flags = LLVMDIFlags.LLVMDIFlagPublic;
            var paramTypes = new LLVMMetadataRef[parameterTypes.Length];
            var idx = 0;
            foreach(var t in parameterTypes)
            {
                paramTypes[idx++] = t.BackendType;
            }
            return new CompilationDebugType(name, builderRef.CreateSubroutineType(CreateDebugFile(location.File), paramTypes, flags));
        }

        public LLVMMetadataRef CreateDebugFunction(string functionName, SourceLocation location, CompilationFunctionType functionType)
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

        public LLVMMetadataRef CreateLexicalScope(LLVMMetadataRef parentScope, SourceLocation location)
        {
            return builderRef.CreateLexicalBlock(parentScope, CreateDebugFile(location.File), location.StartLine, location.StartColumn);
        }

        public CompilationDebugType CreateBasicType(string name, System.UInt64 numBits, BasicType type)
        {
            var dwarfType = LLVMDwarfATEValues.None;
            switch (type)
            {
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

        public CompilationDebugType CreatePointerType(string name, CompilationDebugType element)
        {
            uint ptrAlign = 0;
            uint ptrAddressSpace = 0;
            var dbgType = builderRef.CreatePointerType(element.BackendType, unit.GetPointerSizeInBits(), ptrAlign, ptrAddressSpace, name);
            return new CompilationDebugType(name, dbgType);
        }

        public LLVMMetadataRef CreateParameterVariable(string name, LLVMMetadataRef parentScope, SourceLocation location, uint argNo, LLVMMetadataRef type)
        {
            return builderRef.CreateParameterVariable(parentScope, name, argNo, CreateDebugFile(location.File), location.StartLine, type);
        }

        public void InsertDeclareAtEnd(CompilationValue storage, LLVMMetadataRef varInfo, SourceLocation location, CompilationBlock block)
        {
            var debugLog = unit.CreateDebugLocationMeta(location);
            builderRef.InsertDeclareAtEnd(storage.BackendValue, varInfo, builderRef.CreateEmptyExpression(), debugLog, block.BackendValue);
        }

        public LLVMMetadataRef CreateAutoVariable(string name, LLVMMetadataRef parentScope, SourceLocation location, CompilationDebugType type)
        {
            var preserveAlways = true;
            var alignBits = 0u;
            var flags = LLVMDIFlags.LLVMDIFlagPublic;

            return builderRef.CreateAutoVariable(parentScope, name, CreateDebugFile(location.File), location.StartLine, type.BackendType, preserveAlways, flags, alignBits);
        }

        public LLVMMetadataRef CreateGlobalVarable(string name, string linkageName, LLVMMetadataRef scope, SourceLocation location, CompilationDebugType type)
        {
            var isVisibleExternally = true;
            LLVMMetadataRef expr = null;
            LLVMMetadataRef decl = null;
            var alignBits = 0u;
            return builderRef.CreateGlobalVariable(scope, name, linkageName, CreateDebugFile(location.File), location.StartLine, type.BackendType, isVisibleExternally, expr, decl, alignBits);
        }


        public LLVMMetadataRef RootScope => debugScope;

        public struct StructLayout 
        {
            public StructLayout(CompilationUnit unit, CompilationStructureType structureType)
            {
                structSize = 0;
                elementOffsets = new UInt64[structureType.Elements.Length];
                // Only handles packed structs!
                var dataLayout = unit.Module.GetDataLayout();
                var numElements = structureType.Elements.Length;
                for (int idx = 0; idx < numElements; idx++)
                {
                    var type = structureType.Elements[idx].BackendType;
                    elementOffsets[idx] = structSize;
                    structSize += dataLayout.GetABISizeOfType(type);
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

            public UInt64 StructSizeInBits()
            {
                return structSize * 8;
            }

            UInt64 structSize;
            UInt64[] elementOffsets;
        }

        public StructLayout GetStructLayout(CompilationStructureType structType)
        {
            return new StructLayout(unit, structType);
        }
        public CompilationDebugType CreateStructureType(string name, CompilationStructureType structType)
        {
            var dataLayout = unit.Module.GetDataLayout();
            var structLayout = GetStructLayout(structType);
            var structElements = new LLVMMetadataRef[structType.Elements.Length];
            var alignBits = 8u;  // structures are always packed at present
            var flags = LLVMDIFlags.LLVMDIFlagPublic;

            for (int idx = 0; idx < structType.Elements.Length; idx++)
            {
                var offsetBits = structLayout.GetElementOffsetInBits(idx);
                var sizeBits = dataLayout.GetTypeSizeInBits(structType.Elements[idx].BackendType);
                var location = structType.Elements[idx].Location;
                var dbgType = builderRef.CreateStructElement(debugScope, structType.Fields[idx],CreateDebugFile(location.File), location.StartLine, sizeBits, alignBits, offsetBits, flags, structType.Elements[idx].DebugType.BackendType);
                structElements[idx] = dbgType;
            }

            var structSizeBits = structLayout.StructSizeInBits();
            var structureType = builderRef.CreateStruct(debugScope, name, CreateDebugFile(structType.Location.File), structType.Location.StartLine, structSizeBits, alignBits, flags, structElements);
            return new CompilationDebugType(name, structureType);
        }

        public CompilationDebugType CreateArrayType(string name, CompilationArrayType arrayType)
        {
            var dataLayout = unit.Module.GetDataLayout();
            var subscripts = new LLVMMetadataRef[1];
            subscripts[0] = builderRef.GetOrCreateSubrange(0, arrayType.ElementCount);
            var sizeBits = dataLayout.GetTypeSizeInBits(arrayType.BackendType);
            var alignBits = 8u;
            var dbgType = builderRef.CreateArray(sizeBits, alignBits, arrayType.ElementType.DebugType.BackendType, subscripts);
            return new CompilationDebugType(name, dbgType);
        }

        public CompilationDebugType CreateEnumType(string name, CompilationEnumType enumType)
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
            var alignBits = dataLayout.GetABIAlignmentOfType(enumType.ElementType.BackendType);
            var dbgType = builderRef.CreateEnum(debugScope, name, CreateDebugFile(enumType.Location.File), enumType.Location.StartLine, sizeBits, alignBits, elements);
            return new CompilationDebugType(name, dbgType);
        }


        public void Finalise()
        {
            builderRef.DIBuilderFinalize();
        }

    }
}