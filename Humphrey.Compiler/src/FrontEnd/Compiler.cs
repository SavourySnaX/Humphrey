using Humphrey.Backend;
using System.Collections.Generic;

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

        public CompilationUnit Compile(SemanticPass pass, string sourceFileNameAndPath, string targetTriple, bool disableOptimisations, bool debugInfo, Dictionary<string, string> defines = null)
        {
            var unit = new CompilationUnit(sourceFileNameAndPath, pass.RootSymbolTable, pass.ImportedNamespaces, pass.Manager, pass.ToCompile, targetTriple, disableOptimisations, debugInfo, messages);
            if (defines != null)
            {
                foreach (var kp in defines)
                {
                    unit.PreDefined.Add(kp.Key, kp.Value);
                }
            }
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