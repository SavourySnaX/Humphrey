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
            var parser = new HumphreyParser(tokens);
            var (success, parsed) = parser.IdentifierList();
            Assert.True(success);

            if (expected == null)
                expected = new string[0];

            Assert.True(parsed.Length == expected.Length);
            for (int a = 0; a < parsed.Length; a++)
            {
                Assert.True(parsed[a] == expected[a]);
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
            var parser = new HumphreyParser(tokens);
            var (success, parsed) = parser.NumberList();
            Assert.True(success);

            if (expected == null)
                expected = new string[0];

            Assert.True(parsed.Length == expected.Length);
            for (int a = 0; a < parsed.Length; a++)
            {
                Assert.True(parsed[a] == expected[a]);
            }
        }

        [Theory]
        [InlineData("0+5","+ 0 5")]
        [InlineData("a+b","+ a b")]
        [InlineData("a+b+c","+ + a b c")]
        [InlineData("a+b-c","- + a b c")]
        [InlineData("(a+b)-c","- + a b c")]
        [InlineData("((a)+(b))-(c)","- + a b c")]
        [InlineData("a","a")]
        [InlineData("22","22")]
        [InlineData("+1","+ 1")]
        [InlineData("+5","+ 5")]
        [InlineData("-3","- 3")]
        [InlineData("-a","- a")]
        [InlineData("-(3+4)","- + 3 4")]
        [InlineData("7-3-2","- - 7 3 2")]
        [InlineData("6/3","/ 6 3")]
        [InlineData("51 *   94","* 51 94")]
        [InlineData("1+2*3","+ 1 * 2 3")]
        [InlineData("2*3+1","+ * 2 3 1")]
        [InlineData("01₂+10₂+100₂","+ + 1 2 4")]
        public void CheckExpression(string input, string expected)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens);
            var (success, parsed) = parser.ParseExpression();
            if (null == expected)
            {
                Assert.False(success);
            }
            else
            {
                Assert.True(success);
                Assert.True(parsed == expected);
            }
        }

        [Theory]
        [InlineData("bit","bit")]
        [InlineData("a","a")]
        [InlineData("0",null)]
        public void CheckType(string input, string expected)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens);
            var (success, parsed) = parser.Type();
            if (null == expected)
            {
                Assert.False(success);
            }
            else
            {
                Assert.True(success);
                Assert.True(parsed == expected);
            }
        }
        
        [Theory]
        [InlineData("a : bit","a : bit")]
        [InlineData("a : a","a : a")]       // Semantically incorrect
        [InlineData("0",null)]
        public void CheckParamDefinition(string input, string expected)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens);
            var (success, parsed) = parser.ParamDefinition();
            if (null == expected)
            {
                Assert.False(success);
            }
            else
            {
                Assert.True(success);
                Assert.True(parsed == expected);
            }
        }
        
        [Theory]
        [InlineData("a:bit","a : bit")]
        [InlineData("a:a","a : a")]             // Semantically incorrect
        [InlineData("bitval   :bit= 1", "bitval : bit = 1")]
        [InlineData("bitval=1","bitval = 1")]
        [InlineData("bitval=1*0","bitval = * 1 0")]
        [InlineData("bitval=a","bitval = a")]
        [InlineData("a:0",null)]
        [InlineData("a=bit",null)]
        public void CheckDefinition(string input, string expected)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens);
            var (success, parsed) = parser.Definition();
            if (null == expected)
            {
                Assert.False(success);
            }
            else
            {
                Assert.True(success);
                Assert.True(parsed == expected);
            }
        }
        
        [Theory]
        [InlineData("a:bit",new []{"a : bit"})]
        [InlineData("a:bit,b:bit",new []{"a : bit","b : bit"})]
        [InlineData("a:bit,b:thing",new []{"a : bit","b : thing"})]
        [InlineData("a:bit,b:0",null)]
        public void CheckParamDefinitionList(string input, string[] expected)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens);
            var (success, parsed) = parser.ParamDefinitionList();
            if (null == expected)
            {
                Assert.False(success);
            }
            else
            {
                Assert.True(success);
                Assert.True(parsed.Length == expected.Length);
                for (int a = 0; a < parsed.Length;a++)
                    Assert.True(parsed[a] == expected[a]);
            }
        }
        
        [Theory]
        [InlineData("()","")]
        [InlineData("(a : bit)","a : bit")]
        [InlineData("(a:bit,b:bit)","a : bit , b : bit")]
        public void CheckParamList(string input, string expected)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens);
            var (success, parsed) = parser.ParamList();
            if (null == expected)
            {
                Assert.False(success);
            }
            else
            {
                Assert.True(success);
                Assert.True(parsed == expected);
            }
        }
        
    }
}
