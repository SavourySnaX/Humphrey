using Xunit;

using sly.parser.generator;

namespace Humphrey.FrontEnd.tests
{
    public class ParserTests
    {
        [Theory]
        [InlineData("bit Buffer (bit a) { return a; } #single line comment\n")]
        public void VerifyParser(string testProgram)
        {
            var parser = new Parser();
            var builder = new ParserBuilder<Tokens, string>();
            var built = builder.BuildParser(parser, ParserType.EBNF_LL_RECURSIVE_DESCENT, "function");
            Assert.False(built.IsError);
            var result = built.Result.Parse(testProgram);
            Assert.False(result.IsError);
        }
    }
}
