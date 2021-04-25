using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class HumphreyCompiler
    {
        CompilerMessages messages;
        public HumphreyCompiler(CompilerMessages overrideDefaultMessages = null)
        {
            messages = overrideDefaultMessages;
            if (messages == null)
                messages = new CompilerMessages(true, true, false);
        }

        public CompilationUnit Compile(CommonSymbolTable symbols, IPackageManager manager, IGlobalDefinition[] definitions, string sourceFileNameAndPath , string targetTriple, bool disableOptimisations, bool debugInfo)
        {
            var unit = new CompilationUnit(sourceFileNameAndPath, symbols, manager, definitions, targetTriple, disableOptimisations, debugInfo, messages);
            try
            {
                unit.Compile();
            }
            catch (CompilationAbortException cae)
            {
                messages.Log(CompilerErrorKind.Error_CompilationAborted, $"Compilation Aborted '{cae.Message}'");
            }
            return unit;
        }
    }
}