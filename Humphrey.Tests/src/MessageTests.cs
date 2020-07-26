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
            var messages = new CompilerMessages(true, true, false);
            var tokenise = new HumphreyTokeniser(messages);
            var tokens = tokenise.Tokenize(input);
            var list = tokens.ToList();
            if (expected == CompilerErrorKind.Debug)
                Assert.True(messages.Dump().Length == 0, $"No compiler messages should have been generated but got {messages.Dump()}");
            else
                Assert.True(messages.HasMessageKindBeenLogged(expected), $"Expected message code {(uint)expected:D4} but was not found");
        }
    }
}

