using Xunit;

using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Runtime.CompilerServices;
using System;

namespace Humphrey.FrontEnd.tests
{
    public class LangTests
    {
        public class SourceFileDataSource : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                return GetEnumerator("");
            }
            public IEnumerator<object[]> GetEnumerator(string dummy, [CallerFilePath] string rootFilePath = "")
            {
                var pathsToScan = new Stack<string>();

                pathsToScan.Push(Path.GetDirectoryName(rootFilePath));

                while (pathsToScan.Count>0)
                {
                    var current = pathsToScan.Pop();
                    foreach (var dir in Directory.GetDirectories(current))
                    {
                        pathsToScan.Push(dir);
                    }
                    foreach (var file in Directory.GetFiles(current,"*.humphrey"))
                    {
                        yield return new object[] { File.ReadAllText(file)+Environment.NewLine };
                    }
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        [Theory]
        [ClassData(typeof(SourceFileDataSource))]
        public void RunSourceFileTest(string testProgram)
        {
            /*
            var parser = new Parser();
            var builder = new ParserBuilder<Tokens, string>();
            var built = builder.BuildParser(parser, ParserType.EBNF_LL_RECURSIVE_DESCENT, "file");
            Assert.False(built.IsError);
            var result = built.Result.Parse(testProgram);
            Assert.False(result.IsError);
            */
        }
    }
}
