using Humphrey.FrontEnd;
using System.Linq;
using Xunit;

namespace Humphrey.Tests.src
{
    public class TokenTests
    {
        [Theory]
        [InlineData("#", Tokens.SingleComment)]
        [InlineData("#####", Tokens.SingleComment)]
        [InlineData("# return", Tokens.SingleComment)]
        [InlineData("# This is a whole line of comment", Tokens.SingleComment)]
        [InlineData("#! This is a whole block of comment !#", Tokens.MultiLineComment)]
        public void CheckComments(string input, Tokens expected)
        {
            TokenTest(input, new[] { expected });
        }

        [Theory]
        [InlineData("\"\"", new []{Tokens.String})]
        [InlineData("\"some sort of string\"", new []{Tokens.String})]
        [InlineData("\"F₁₆\"", new []{Tokens.String})]
        [InlineData("\"if\"", new []{Tokens.String})]
        [InlineData("\"毛唐\"", new []{Tokens.String})]
        [InlineData(@"""Hello World"";", new []{Tokens.String, Tokens.S_SemiColon})]
        public void CheckStringLiterals(string input, Tokens[] expected)
        {
            TokenTest(input, expected);
        }

        [Theory]
        [InlineData("+", Tokens.O_Plus)]
        [InlineData("-", Tokens.O_Subtract)]
        [InlineData("*", Tokens.O_Multiply)]
        [InlineData("/", Tokens.O_Divide)]
        [InlineData("%", Tokens.O_Modulus)]
        [InlineData("=", Tokens.O_Equals)]
        [InlineData(":", Tokens.O_Colon)]
        [InlineData(".", Tokens.O_Dot)]
        [InlineData("..", Tokens.O_DotDot)]
        [InlineData("==", Tokens.O_EqualsEquals)]
        [InlineData("++", Tokens.O_PlusPlus)]
        [InlineData("--", Tokens.O_MinusMinus)]
        [InlineData("!=", Tokens.O_NotEquals)]
        [InlineData("<=", Tokens.O_LessEquals)]
        [InlineData(">=", Tokens.O_GreaterEquals)]
        [InlineData("!", Tokens.O_LogicalNot)]
        [InlineData("~", Tokens.O_BinaryNot)]
        [InlineData("&", Tokens.O_BinaryAnd)]
        [InlineData("|", Tokens.O_BinaryOr)]
        [InlineData("^", Tokens.O_BinaryXor)]
        [InlineData("&&", Tokens.O_LogicalAnd)]
        [InlineData("||", Tokens.O_LogicalOr)]
        [InlineData("<", Tokens.O_Less)]
        [InlineData(">", Tokens.O_Greater)]
        [InlineData("as", Tokens.O_As)]
        public void CheckOperatorTokens(string input, Tokens expected)
        {
            TokenTest(input, new[] { expected });
        }

        [Theory]
        [InlineData("{", Tokens.S_OpenCurlyBrace)]
        [InlineData("}", Tokens.S_CloseCurlyBrace)]
        [InlineData("(", Tokens.S_OpenParanthesis)]
        [InlineData(")", Tokens.S_CloseParanthesis)]
        [InlineData("[", Tokens.S_OpenSquareBracket)]
        [InlineData("]", Tokens.S_CloseSquareBracket)]
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
        [InlineData("for", Tokens.KW_For)]
        [InlineData("if", Tokens.KW_If)]
        [InlineData("else", Tokens.KW_Else)]
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
        [InlineData("0", Tokens.Number)]
        [InlineData("1", Tokens.Number)]
        [InlineData("00", Tokens.Number)]
        [InlineData("01", Tokens.Number)]
        [InlineData("1_000_000", Tokens.Number)]
        [InlineData("0xF", Tokens.Number)]
        [InlineData("0b1010", Tokens.Number)]
        [InlineData("0b_1010_0011", Tokens.Number)]
        [InlineData(@"F\_16", Tokens.Number)]
        [InlineData("F₁₆", Tokens.Number)]
        [InlineData("DE_AD_BE_EF₁₆", Tokens.Number)]
        [InlineData("18₉", Tokens.Number)]
        [InlineData("10101₂", Tokens.Number)]
        public void CheckNumbers(string input, Tokens expected)
        {
            TokenTest(input, new[] { expected });
        }

        [Theory]
        [InlineData("__", new[] { Tokens.S_Underscore, Tokens.S_Underscore })]
        [InlineData("#!!#_", new[] { Tokens.MultiLineComment, Tokens.S_Underscore })]
        [InlineData("#!#!!#!#", new[] { Tokens.MultiLineComment })]
        [InlineData("#!!##!!#", new[] { Tokens.MultiLineComment, Tokens.MultiLineComment })]
        public void CheckOutliers(string input, Tokens[] tokens)
        {
            TokenTest(input, tokens);
        }

        private void TokenTest(string input, Tokens[] expected)
        {
            var messages = new CompilerMessages(false, true, false);
            var tokenise = new HumphreyTokeniser(messages);
            var tokens = tokenise.Tokenize(input);
            var list = tokens.ToList();
            Assert.False(messages.HasErrors, $"Tokeniser failed due to : {messages.Dump()}");
            Assert.True(list.Count == expected.Length, $"'{input}' Expected {expected.Length} items, got {list.Count}");
            for (int a = 0; a < list.Count; a++)
            {
                Assert.True(list[a].Value == expected[a], $"'{input}' Expected {expected[a]} but got {list[a].Value}");
            }
        }


    }
}
