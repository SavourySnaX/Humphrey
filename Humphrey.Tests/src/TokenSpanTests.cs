using Humphrey.FrontEnd;
using System.Linq;
using Xunit;

namespace Humphrey.FrontEnd.Tests
{
    public class TokenSpanTests
    {
        [Theory]
        [InlineData("", 1, 1)]
        [InlineData("a", 1, 2)]
        [InlineData("Ꜳ", 1, 2)]
        [InlineData("abcdef", 1, 7)]
        [InlineData("DE_AD_BE_EF₁₆", 1, 14)]
        [InlineData("\r", 2, 1)]
        [InlineData("\n", 2, 1)]
        [InlineData("\r\n", 2, 1)]
        [InlineData("\r\n\r", 3, 1)]
        [InlineData("\r\n\n", 3, 1)]
        [InlineData("\r\n\r\n", 3, 1)]
        [InlineData("\u2028", 2, 1)]
        [InlineData(@"# Returns the value passed into it

Main : (inputValue : bit) (returnValue : bit) =
{
    returnValue = inputValue
}", 6, 2)]
        [InlineData(@"#!
public enum FrameBufferType : byte
{
    ARGB=0,
    RGBA=1,
    ABGR=2,
    BGRA=3
}
!#
",10,1)]
        public void CheckEndPosition(string input, int expectedLine,int expectedColumn)
        {
            TokenTest(input, expectedLine, expectedColumn);
        }

        const string docLineLongTest = @"bob:bit #This is documentation
#! blah !# cat:bit #Another doc
";

        [Theory]
        [InlineData("bob:bit #This is documentation", "bit", "This is documentation")]
        [InlineData("bob:bit #!This is documentation", "bit", "")]
        [InlineData(docLineLongTest, "bob", "This is documentation")]
        [InlineData(docLineLongTest, "cat", "Another doc")]
        public void CheckDocFetch(string input, string from, string expectedOutput)
        {
            var tokenised = new HumphreyTokeniser().Tokenize(input);
            foreach (var token in tokenised)
            {
                if (token.ToStringValue() == from)
                {
                    Assert.True(expectedOutput == token.FetchDocLine());
                }
            }
        }

        private void TokenTest(string input, int expectedLine, int expectedColumn)
        {
            var tokens = new TokenSpan("", input, 0, 1, 1);
            Result<char> result = tokens.ConsumeChar();
            while (true)
            {
                result = result.Remainder.ConsumeChar();
                if (!result.HasValue)
                    break;
            }

            var passed = result.Remainder.Line == expectedLine && result.Remainder.Column == expectedColumn;

            Assert.True(passed, $"'{input}' Expected final position to be {expectedLine}:{expectedColumn}, got {result.Location.Line}:{result.Location.Column}");
        }


    }
}

