using System.Collections.Generic;
using Humphrey.FrontEnd;

namespace Humphrey.Backend
{
    public struct SourceLocation
    {
        private string filePath;
        private uint startLine, startColumn, endLine, endColumn;
        Result<Tokens> frontendLocation;

        public SourceLocation(uint parameterless=0)
        {
            filePath = "";
            startLine = startColumn = endLine = endColumn = 0;
            frontendLocation = new Result<Tokens>();
        }

        public SourceLocation(Result<Tokens> location)
        {
            filePath = location.Location.Filename;
            startLine = location.Location.Line;
            startColumn = location.Location.Column;
            endLine = location.Remainder.Line;
            endColumn = location.Remainder.Column;
            frontendLocation = location;
        }

        public bool Valid => !(startLine == endLine && endLine == startColumn && endColumn == startLine && startLine == 0);

        public string File => filePath;
        public uint StartLine => startLine;
        public uint StartColumn => startColumn;
        public uint EndLine => endLine;
        public uint EndColumn => endColumn;

        public Result<Tokens> FrontendLocation => frontendLocation;
    }
}