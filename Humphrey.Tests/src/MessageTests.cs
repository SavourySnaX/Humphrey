using Humphrey.Backend;
using Humphrey.FrontEnd;
using System.Linq;
using Xunit;

namespace Humphrey.Tests.src
{
    public class MessageTests
    {
        [Theory]
        [InlineData("#", CompilerErrorKind.Debug)]
        [InlineData("\b", CompilerErrorKind.Error_InvalidToken)]
        public void CheckComments(string input, CompilerErrorKind kind)
        {
            TokenTest(input, kind);
        }

        private void TokenTest(string input, CompilerErrorKind expected)
        {
            var messages = new CompilerMessages(false, true, false);
            var tokenise = new HumphreyTokeniser(messages);
            var tokens = tokenise.Tokenize(input);
            var list = tokens.ToList();
            if (expected == CompilerErrorKind.Debug)
                Assert.True(messages.Dump().Length == 0, $"No compiler messages should have been generated but got {messages.Dump()}");
            else
                Assert.True(messages.HasMessageKindBeenLogged(expected), $"Expected message code {(uint)expected:D4} but was not found");
        }
        
        [Theory]
        [InlineData("WorkingFunction:()(out:bit)={out=0}", CompilerErrorKind.Debug)]
        [InlineData("BrokenFunction:()(out:bit)={}", CompilerErrorKind.Error_MissingOutputAssignment)]
        [InlineData("BrokenFunction:()(out1:bit,out2:bit)={out1=0}", CompilerErrorKind.Error_MissingOutputAssignment)]
        [InlineData("BrokenFunction:()(out1:bit,out2:bit)={out2=0}", CompilerErrorKind.Error_MissingOutputAssignment)]
        [InlineData("BrokenFunction:()(out1:bit,out2:bit)={}", CompilerErrorKind.Error_MissingOutputAssignment)]
        [InlineData("BrokenFunction:()(out1:bit,out2:bit)={out1,out2=0}", CompilerErrorKind.Debug)]
        public void CheckCompilationMessages(string input, CompilerErrorKind kind)
        {
            CompilationTest(input, kind);
        }

        private void CompilationTest(string input, CompilerErrorKind expected)
        {
            var messages = new CompilerMessages(true, true, false);
            var tokenise = new HumphreyTokeniser(messages);
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens, messages);
            var parsed = parser.File();
            var compiler = new CompilationUnit("test", messages);
            foreach (var def in parsed)
            {
                def.Compile(compiler);
            }
            if (expected == CompilerErrorKind.Debug)
                Assert.True(messages.Dump().Length == 0, $"No compiler messages should have been generated but got {messages.Dump()}");
            else
                Assert.True(messages.HasMessageKindBeenLogged(expected), $"Expected message code {(uint)expected:D4} ({expected}) but was not found");

            var s = messages.Dump();
        }
    }
}

