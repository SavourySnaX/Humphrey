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

        public CompilationUnit Compile(IGlobalDefinition[] definitions, string moduleName )
        {
            var unit = new CompilationUnit(moduleName, definitions, messages);
            unit.Compile();
            return unit;
        }
    }
}