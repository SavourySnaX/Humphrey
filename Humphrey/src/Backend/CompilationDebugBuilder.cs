using System.IO;
using LLVMSharp.Interop;
using static Extensions.Helpers;

namespace Humphrey.Backend
{
    public class CompilationDebugBuilder
    {
        LLVMDIBuilderRef builderRef;
        LLVMMetadataRef debugCU;

        public CompilationDebugBuilder(CompilationUnit unit, string fileNameAndPath, string compilerVersion, bool isOptimised)
        {
            string flags = "";
            string splitName = "";
            uint runtimeVersion = 0;
            uint dwOld = 0;
            int splitDebugInlining = 1;
            int debugInfoForProfiling = 1;
            builderRef = CreateDIBuilder(unit.Module);

            unit.AddModuleFlag(LLVMModuleFlagBehavior.LLVMModuleFlagBehaviorWarning, "Debug Info Version", GetDebugMetaVersion());

            //TODO will need to select codeview/dwarf output - for now just use dwarf (old version)
            unit.AddModuleFlag(LLVMModuleFlagBehavior.LLVMModuleFlagBehaviorWarning, "Dwarf Version", 2);

            //Finally we need to tag the llvm.ident
            unit.AddNamedMetadata("llvm.ident", compilerVersion);

            debugCU = builderRef.CreateCompileUnit(LLVMDWARFSourceLanguage.LLVMDWARFSourceLanguageC,
                CreateDebugFile(Path.GetFileName(fileNameAndPath), Path.GetDirectoryName(Path.GetFullPath(fileNameAndPath))),
                compilerVersion, isOptimised ? 1 : 0, flags, runtimeVersion, splitName, LLVMDWARFEmissionKind.LLVMDWARFEmissionFull,
                dwOld, splitDebugInlining, debugInfoForProfiling);
        }

        private LLVMMetadataRef CreateDebugFile(string filename, string path)
        {
            return builderRef.CreateFile(filename, path);
        }
    }
}