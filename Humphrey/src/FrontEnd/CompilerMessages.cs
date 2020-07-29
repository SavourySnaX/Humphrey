using System.Collections.Generic;
using System.Text;

namespace Humphrey.FrontEnd
{
    public enum CompilerErrorKind : ushort
    {
        // Tokeniser block
        Error_InvalidToken              =   Error|0x001,
        // Parser block
        // Compilation block
        Error_MissingOutputAssignment   =   Error|0x801,
        // LLVM block
        Error_FailedVerification        =   Error|0xC01,

        Debug=0x0000,
        Info = 0x4000,
        Warning=0x8000,
        Error=0xC000,

        KindMask=0xC000

    }

    public class CompilerMessages
    {
        private List<(CompilerErrorKind errorKind, string message, TokenSpan? location)> messages;
        private HashSet<CompilerErrorKind> logged;
        bool anyErrorsLogged;
        bool logDebug;
        bool logInfo;
        bool warningsErrors;

        public CompilerMessages(bool debugEnable, bool infoEnable, bool warningsAsErrors)
        {
            messages = new List<(CompilerErrorKind errorKind, string message, TokenSpan? location)>();
            logged = new HashSet<CompilerErrorKind>();
            anyErrorsLogged = false;
            logDebug = debugEnable;
            logInfo = infoEnable;
            warningsErrors = warningsAsErrors;
        }

        public bool HasMessageKindBeenLogged(CompilerErrorKind kind)
        {
            return logged.Contains(kind&~CompilerErrorKind.KindMask);
        }

        public void Log(CompilerErrorKind kind, string message)
        {
            Log(kind, message, null);
        }
        public void Log(CompilerErrorKind kind, string message, TokenSpan? location)
        {
            var type = kind & CompilerErrorKind.KindMask;
            var code = kind & ~CompilerErrorKind.KindMask;
            if (type == CompilerErrorKind.Debug && !logDebug)
                return;
            if (type == CompilerErrorKind.Info && !logInfo)
                return;
            if (type == CompilerErrorKind.Warning && warningsErrors)
            {
                type = CompilerErrorKind.Error;
                kind = code | CompilerErrorKind.Error;
            }
            messages.Add((kind, message, location));
            anyErrorsLogged |= type == CompilerErrorKind.Error;
            logged.Add(code);
        }

        public string Dump()
        {
            var s = new StringBuilder();
            foreach (var m in messages)
            {
                var type = m.errorKind & CompilerErrorKind.KindMask;
                var code = m.errorKind & ~CompilerErrorKind.KindMask;
                switch (type)
                {
                    case CompilerErrorKind.Warning:
                        s.Append("Warning [W");
                        break;
                    case CompilerErrorKind.Debug:
                        s.Append("Debug [D");
                        break;
                    case CompilerErrorKind.Info:
                        s.Append("Info [I");
                        break;
                    case CompilerErrorKind.Error:
                        s.Append("Error [E");
                        break;
                }
                s.Append($"{(uint)code:D4}]: ");
                if (m.location.HasValue)
                {
                    s.AppendLine($"{m.message}{System.Environment.NewLine}\t--> {m.location.Value}");
                    s.AppendLine();
                    s.AppendLine(m.location.Value.DumpContext());
                    s.AppendLine();
                }
                else
                {
                    s.AppendLine($"{m.message}");
                    s.AppendLine();
                }
            }
            return s.ToString();
        }

        public bool HasErrors => anyErrorsLogged;
    }
}