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

        public CompilationUnit Compile(IGlobalDefinition[] definitions, string sourceFileNameAndPath , string targetTriple, bool disableOptimisations)
        {
            var unit = new CompilationUnit(sourceFileNameAndPath, definitions, targetTriple, disableOptimisations, messages);
            unit.Compile();
            return unit;
        }
    }
}