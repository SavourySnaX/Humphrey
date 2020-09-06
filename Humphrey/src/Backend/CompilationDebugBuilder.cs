using System.IO;
using System.Text;
using LLVMSharp.Interop;
using static Extensions.Helpers;

namespace Humphrey.Backend
{
    public class CompilationDebugBuilder
    {
        LLVMDIBuilderRef builderRef;
        LLVMMetadataRef debugCU;
        LLVMMetadataRef debugScope;
        bool optimised;

        public CompilationDebugBuilder(CompilationUnit unit, string fileNameAndPath, string compilerVersion, bool isOptimised)
        {
            string flags = "";
            string splitName = "";
            uint runtimeVersion = 0;
            uint dwOld = 0;
            int splitDebugInlining = 1;
            int debugInfoForProfiling = 1;
            
            optimised = isOptimised;
            builderRef = CreateDIBuilder(unit.Module);

            unit.AddModuleFlag(LLVMModuleFlagBehavior.LLVMModuleFlagBehaviorWarning, "Debug Info Version", GetDebugMetaVersion());

            //TODO will need to select codeview/dwarf output - for now just use dwarf (old version)
            unit.AddModuleFlag(LLVMModuleFlagBehavior.LLVMModuleFlagBehaviorWarning, "Dwarf Version", 2);

            //Finally we need to tag the llvm.ident
            unit.AddNamedMetadata("llvm.ident", compilerVersion);

            debugScope = CreateDebugFile(fileNameAndPath);
            debugCU = builderRef.CreateCompileUnit(LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageC,
                debugScope,
                compilerVersion, isOptimised ? 1 : 0, flags, runtimeVersion, splitName, LLVMDWARFEmissionKind.LLVMDWARFEmissionFull,
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

        public LLVMMetadataRef CreateDebugFunctionType(CompilationFunctionType functionType, SourceLocation location)
        {
            //TODO - parameters
            var parameters = new LLVMMetadataRef[] { null };
            var flags = LLVMDIFlags.LLVMDIFlagPublic;
            return builderRef.CreateSubroutineType(CreateDebugFile(location.File), parameters, flags);
        }

        public LLVMMetadataRef CreateDebugFunction(string functionName, SourceLocation location, CompilationFunctionType functionType, SourceLocation typeLocation)
        {
            var localToUnit = 0;
            var definition = 1;
            var scopeLine = location.StartLine;
            var flags = LLVMDIFlags.LLVMDIFlagPublic;
            var isOptimised = optimised ? 1 : 0;

            return builderRef.CreateFunction(debugScope, functionName, AsciiSafeName(functionName),
                CreateDebugFile(location.File), location.StartLine, CreateDebugFunctionType(functionType, typeLocation),
                localToUnit, definition, scopeLine, flags, isOptimised);
        }

        public LLVMMetadataRef CreateLexicalScope(LLVMMetadataRef parentScope, SourceLocation location)
        {
            return builderRef.CreateLexicalBlock(parentScope, CreateDebugFile(location.File), location.StartLine, location.StartColumn);
        }

        public LLVMMetadataRef RootScope => debugScope;

        public void Finalise()
        {
            builderRef.DIBuilderFinalize();
        }

    }
}