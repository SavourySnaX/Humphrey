using Humphrey.FrontEnd;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Xunit;

namespace Humphrey.Tests.src
{
    public class TokenTests
    {
        [Theory]
        [InlineData("+", Tokens.O_Plus)]
        public void CheckOperatorTokens(string input, Tokens expected)
        {
            TokenTest(input, expected);
        }

        [Theory]
        [InlineData("{", Tokens.S_OpenCurlyBrace)]
        [InlineData("}", Tokens.S_CloseCurlyBrace)]
        [InlineData("(", Tokens.S_OpenParanthesis)]
        [InlineData(")", Tokens.S_CloseParanthesis)]
        [InlineData(",", Tokens.S_Comma)]
        [InlineData(";", Tokens.S_SemiColon)]
        public void CheckSyntaxTokens(string input, Tokens expected)
        {
            TokenTest(input, expected);
        }

        [Theory]
        [InlineData("return", Tokens.KW_Return)]
        [InlineData("bit", Tokens.KW_Bit)]
        public void CheckKeywordTokens(string input, Tokens expected)
        {
            TokenTest(input, expected);
        }

        private void TokenTest(string input, Tokens expected)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var list = tokens.ToList();
            Assert.True(list.Count == 1);
            Assert.True(list[0].Kind == expected);
        }

    }
}
