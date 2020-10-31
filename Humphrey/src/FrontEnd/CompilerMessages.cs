using System.Collections.Generic;
using System.Text;

namespace Humphrey.FrontEnd
{
    public enum CompilerErrorKind : ushort
    {
        // Tokeniser block
        Error_InvalidToken = Error | TokeniseError | 0x01,
        // Parser block
        Error_ExpectedGlobalDefinition = Error | ParseError | 0x01,
        Error_ExpectedEnumMemberDefinition = Error | ParseError | 0x02,
        Error_ExpectedStructMemberDefinition = Error | ParseError | 0x03,
        Error_ExpectedAssignable = Error | ParseError | 0x04,
        // Compilation block
        Error_MissingOutputAssignment = Error | CompileError | 0x01,
        Error_IntegerWidthMismatch = Error | CompileError | 0x02,
        Error_UndefinedType = Error | CompileError | 0x03,
        Error_UndefinedValue = Error | CompileError | 0x04,
        Error_TypeMismatch = Error | CompileError | 0x05,
        Error_CompilationAborted = Error | CompileError | 0xFF,
        // LLVM block
        Error_FailedVerification = Error | LLVMError | 0x01,

        Debug = 0x0000,
        Info = 0x4000,
        Warning = 0x8000,
        Error = 0xC000,

        TokeniseError = 0x000,
        ParseError = 0x400,
        CompileError = 0x800,
        LLVMError = 0xC00,

        KindMask = 0xC000,
        ErrorKindMask = 0x0C00,
    }

    public class CompilerMessages
    {
        private List<(CompilerErrorKind errorKind, string message, TokenSpan? location, TokenSpan? remain)> messages;
        private HashSet<CompilerErrorKind> logged;
        bool anyErrorsLogged;
        bool logDebug;
        bool logInfo;
        bool warningsErrors;

        public CompilerMessages(bool debugEnable, bool infoEnable, bool warningsAsErrors)
        {
            messages = new List<(CompilerErrorKind errorKind, string message, TokenSpan? location, TokenSpan? remain)>();
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
            Log(kind, message, null, null);
        }
        public void Log(CompilerErrorKind kind, string message, TokenSpan? location)
        {
            Log(kind, message, location, null);
        }
        public void Log(CompilerErrorKind kind, string message, TokenSpan? location, TokenSpan? remain)
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
            messages.Add((kind, message, location, remain));
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
                    s.AppendLine(m.location.Value.DumpContext(m.remain));
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