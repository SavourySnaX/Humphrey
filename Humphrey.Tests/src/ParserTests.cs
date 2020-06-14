using Xunit;

namespace Humphrey.FrontEnd.tests
{
    public class ParserTests
    {
        [Theory]
        [InlineData("main", new[] { "main" })]
        [InlineData("main bob", new[] { "main", "bob" })]
        [InlineData("a     b c d    e", new[] { "a", "b","c","d","e" })]
        [InlineData("main + bob", new[] { "main" })]
        [InlineData("+", null)]
        [InlineData("return bit", null)]
        public void CheckIdentifierList(string input, string[] expected)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var parsed = HumphreyParser.IdentifierList.Invoke(tokens);
            Assert.True(parsed.HasValue);

            if (expected == null)
                expected = new string[0];

            Assert.True(parsed.Value.Length == expected.Length);
            for (int a = 0; a < parsed.Value.Length; a++)
            {
                Assert.True(parsed.Value[a] == expected[a]);
            }
        }

        [Theory]
        [InlineData("0", new[] { "0" })]
        [InlineData("1", new[] { "1" })]
        [InlineData("1_000_000", new[] { "1000000" })]
        [InlineData("$F", new[] { "15" })]
        [InlineData("%1010", new[] { "10" })]
        [InlineData("%1010_0011", new[] { "163" })]
        [InlineData(@"F\_16", new[] { "15" })]
        [InlineData("F₁₆", new[] { "15" })]
        [InlineData("DE_AD_BE_EF₁₆", new[] { "3735928559" })]
        [InlineData("18₉", new[] { "17" })]
        [InlineData("10101₂", new[] { "21" })]
        [InlineData("0₂0", new[] { "0", "0" })]
        [InlineData("9 5 2", new[] { "9", "5", "2" })]
        public void CheckNumberList(string input, string[] expected)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var parsed = HumphreyParser.NumberList.Invoke(tokens);
            Assert.True(parsed.HasValue);

            if (expected == null)
                expected = new string[0];

            Assert.True(parsed.Value.Length == expected.Length);
            for (int a = 0; a < parsed.Value.Length; a++)
            {
                Assert.True(parsed.Value[a] == expected[a]);
            }
        }

    }
}
