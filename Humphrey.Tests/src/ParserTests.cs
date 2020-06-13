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
    }
}
