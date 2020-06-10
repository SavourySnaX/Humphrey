using Humphrey.FrontEnd;
using System.Linq;
using Xunit;

namespace Humphrey.Tests.src
{
    public class TokenTests
    {
        [Theory]
        [InlineData("+", Tokens.O_Plus)]
        public void CheckOperatorTokens(string input, Tokens expected)
        {
            TokenTest(input, new[] { expected });
        }

        [Theory]
        [InlineData("{", Tokens.S_OpenCurlyBrace)]
        [InlineData("}", Tokens.S_CloseCurlyBrace)]
        [InlineData("(", Tokens.S_OpenParanthesis)]
        [InlineData(")", Tokens.S_CloseParanthesis)]
        [InlineData(",", Tokens.S_Comma)]
        [InlineData(";", Tokens.S_SemiColon)]
        [InlineData("_", Tokens.S_Underscore)]
        public void CheckSyntaxTokens(string input, Tokens expected)
        {
            TokenTest(input, new[] { expected });
        }

        [Theory]
        [InlineData("return", Tokens.KW_Return)]
        [InlineData("bit", Tokens.KW_Bit)]
        public void CheckKeywordTokens(string input, Tokens expected)
        {
            TokenTest(input, new[] { expected });
        }

        [Theory]
        [InlineData("returnbit", Tokens.Identifier)]
        [InlineData("Return", Tokens.Identifier)]
        [InlineData("bIt", Tokens.Identifier)]
        [InlineData("_a", Tokens.Identifier)]
        [InlineData("a1", Tokens.Identifier)]
        [InlineData("Ꜳ", Tokens.Identifier)]
        public void CheckIdentiferTokens(string input, Tokens expected)
        {
            TokenTest(input, new[] { expected });
        }

        [Theory]
        [InlineData("__", new[] { Tokens.S_Underscore, Tokens.S_Underscore })]
        public void CheckOutliers(string input, Tokens[] tokens)
        {
            TokenTest(input, tokens);
        }

        private void TokenTest(string input, Tokens[] expected)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var list = tokens.ToList();
            Assert.True(list.Count == expected.Length);
            for (int a = 0; a < list.Count; a++)
            {
                Assert.True(list[a].Kind == expected[a]);
            }
        }


    }
}
