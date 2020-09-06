using System.Collections.Generic;
using Humphrey.FrontEnd;

namespace Humphrey.Backend
{
    public struct SourceLocation
    {
        private string filePath;
        private uint startLine, startColumn, endLine, endColumn;
        public SourceLocation(Result<Tokens> location)
        {
            filePath = location.Location.Filename;
            startLine = location.Location.Line;
            startColumn = location.Location.Column;
            endLine = location.Remainder.Line;
            endColumn = location.Remainder.Column;
        }

        public string File => filePath;
        public uint StartLine => startLine;
        public uint StartColumn => startColumn;
        public uint EndLine => endLine;
        public uint EndColumn => endColumn;
    }
}