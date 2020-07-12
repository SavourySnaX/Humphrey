﻿using System.Text;
using Xunit;

namespace Humphrey.FrontEnd.tests
{
    public class ParserTests
    {
        [Theory]
        [InlineData("main", new[] { "main" })]
        [InlineData("main_routine", new[] { "main_routine" })]
        [InlineData("main bob", new[] { "main", "bob" })]
        [InlineData("a     b c d    e", new[] { "a", "b","c","d","e" })]
        [InlineData("main + bob", new[] { "main" })]
        [InlineData("+", new string[]{})]
        [InlineData("return bit", new string[]{})]
        [InlineData("_", new string[]{})]
        public void CheckIdentifierList(string input, string[] expected)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens);
            CheckAst(input, parser.IdentifierList(), expected);
        }

        [Theory]
        [InlineData("0", new[] { "0" })]
        [InlineData("1", new[] { "1" })]
        [InlineData("00", new[] { "0" })]
        [InlineData("01", new[] { "1" })]
        [InlineData("1_000_000", new[] { "1000000" })]
        [InlineData("0xF", new[] { "15" })]
        [InlineData("0b1010", new[] { "10" })]
        [InlineData("0b1010_0011", new[] { "163" })]
        [InlineData(@"F\_16", new[] { "15" })]
        [InlineData("F₁₆", new[] { "15" })]
        [InlineData("DE_AD_BE_EF₁₆", new[] { "3735928559" })]
        [InlineData("18₉", new[] { "17" })]
        [InlineData("10101₂", new[] { "21" })]
        [InlineData("0₂0", new[] { "0", "0" })]
        [InlineData("9 5 2", new[] { "9", "5", "2" })]
        [InlineData("DEADBEEF", new string[] { })]
        public void CheckNumberList(string input, string[] expected)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens);
            CheckAst(input, parser.NumberList(), expected);
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
        [InlineData("4%2","% 4 2")]
        [InlineData("51 *   94","* 51 94")]
        [InlineData("1+2*3","+ 1 * 2 3")]
        [InlineData("2*3+1","+ * 2 3 1")]
        [InlineData("1+2%3","+ 1 % 2 3")]
        [InlineData("01₂+10₂+100₂","+ + 1 2 4")]
        public void CheckExpression(string input, string expected)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens);
            CheckAst(input, parser.ParseExpression(), expected);
        }

        [Theory]
        [InlineData("bit","bit")]
        [InlineData("*bit","* bit")]
        [InlineData("*[8]bit","* [8] bit")]
        [InlineData("[1] bit","[1] bit")]
        [InlineData("[8] bit","[8] bit")]
        [InlineData("[-8] bit","[- 8] bit")]
        [InlineData("a","a")]
        [InlineData("[55] a","[55] a")]
        [InlineData("0",null)]
        public void CheckType(string input, string expected)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens);
            CheckAst(input, parser.Type(), expected);
        }
        
        [Theory]
        [InlineData("a : bit","a : bit")]
        [InlineData("a : *bit","a : * bit")]
        [InlineData("a : [1] bit","a : [1] bit")]
        [InlineData("anInt : [-32] bit","anInt : [- 32] bit")]
        [InlineData("aUInt : [32] bit","aUInt : [32] bit")]
        [InlineData("a : a","a : a")]       // Semantically incorrect
        [InlineData("a : [1] a","a : [1] a")]       // Semantically incorrect
        [InlineData("0",null)]
        public void CheckParamDefinition(string input, string expected)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens);
            CheckAst(input, parser.ParamDefinition(), expected);
        }

        [Theory]
        [InlineData("a:bit", "a : bit")]
        [InlineData("a:a", "a : a")]             // Semantically incorrect
        [InlineData("bitval   :bit= 1", "bitval : bit = 1")]
        [InlineData("bitval   :[1]bit= 1", "bitval : [1] bit = 1")]
        [InlineData("bitval=1", "bitval = 1")]
        [InlineData("bitval=1*0", "bitval = * 1 0")]
        [InlineData("bitval=a", "bitval = a")]
        [InlineData("a:0", null)]
        [InlineData("a=bit", null)]
        [InlineData("a=[1] bit", null)]
        [InlineData("FunctionPtr:()()=0", "FunctionPtr : () () = 0")]
        [InlineData("bit:()()=0", null)]
        [InlineData("Main:()(returnVal:bit)", "Main : () (returnVal : bit)")]
        public void CheckDefinition(string input, string expected)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens);
            CheckAst(input, parser.Definition(), expected);
        }

        [Theory]
        [InlineData("a:bit",new []{"a : bit"})]
        [InlineData("a:bit,b:bit",new []{"a : bit","b : bit"})]
        [InlineData("a:bit,b:thing",new []{"a : bit","b : thing"})]
        [InlineData("a:[8]bit,b:thing",new []{"a : [8] bit","b : thing"})]
        [InlineData("a:bit,b:0",null)]
        public void CheckParamDefinitionList(string input, string[] expected)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens);
            CheckAst(input, parser.ParamDefinitionList(), expected);
        }
        
        [Theory]
        [InlineData("()","")]
        [InlineData("(a : bit)","a : bit")]
        [InlineData("(a : [8] bit)","a : [8] bit")]
        [InlineData("(a:bit,b:bit)","a : bit , b : bit")]
        public void CheckParamList(string input, string expected)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens);
            CheckAst(input, parser.ParamList(), expected);
        }
        
        [Theory]
        [InlineData("()",null)]
        [InlineData("()()","() ()")]
        [InlineData("(a:bit)(b:bit,c:bit)","(a : bit) (b : bit , c : bit)")]
        [InlineData("(a:[32]bit)(b:[32]bit,c:[32]bit)","(a : [32] bit) (b : [32] bit , c : [32] bit)")]
        public void CheckFunctionType(string input, string expected)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens);
            CheckAst(input, parser.FunctionType(), expected);
        }

        [Theory]
        [InlineData("[8]bit", "[8] bit")]
        [InlineData("[8][8]bit", "[8] [8] bit")]
        [InlineData("[8]{}}", "[8] { }")]
        [InlineData("[8]bob", "[8] bob")]
        [InlineData("[8]()()", null)]
        public void CheckArrayType(string input, string expected)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens);
            CheckAst(input, parser.ArrayType(), expected);
        }

        [Theory]
        [InlineData("{}","{ }")]
        [InlineData("{bob:bit}","{ bob : bit}")]
        [InlineData("{bob:bit squee:[8]bit}","{ bob : bit squee : [8] bit}")]
        [InlineData("{bob:apple}","{ bob : apple}")]
        [InlineData("{bob:()()}", null)]
        public void CheckStructType(string input, string expected)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens);
            CheckAst(input, parser.StructType(), expected);
        }

        [Theory]
        [InlineData("5+2", new [] {"+ 5 2"})]
        [InlineData("5+2,6-3", new []{"+ 5 2","- 6 3"})]
        [InlineData("a,b,c,d,55", new [] {"a","b","c","d","55"})]
        [InlineData("", null)]
        public void CheckExpressionList(string input, string[] expected)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens);
            CheckAst(input, parser.ExpressionList(), expected);
        }

        [Theory]
        [InlineData("{}","{ }")]
        [InlineData("{return;}","{ return}")]
        [InlineData("{{{}}",null)]
        [InlineData("return;", "return")]
        [InlineData("return", null)]
        [InlineData("return 5+2;", "return + 5 2")]
        [InlineData("return 5+2,6-3;", "return + 5 2 , - 6 3")]
        public void CheckStatement(string input, string expected)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens);
            CheckAst(input, parser.Statement(), expected);
        }

        [Theory]
        [InlineData("{}","{ }")]
        [InlineData("{{{}}}","{ { { }}}")]
        [InlineData("{{{}return;}}","{ { { }return}}")]
        [InlineData("{{{}}",null)]
        public void CheckCodeBlock(string input, string expected)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens);
            CheckAst(input, parser.CodeBlock(), expected);
        }

        void CheckAst(string input, IAst result, string expected)
        {
            if (null == expected)
            {
                Assert.True(result == null, $"'{input}': Expected parse fail, got : '{result?.Dump()}'");
            }
            else
            {
                Assert.True(result != null, $"'{input}': Expected parse success, got failure");
                var res = result.Dump();
                Assert.True(res == expected, $"'{input}': Expected '{expected}' but got '{res}'");
            }
        }

        string DumpAst(IAst[] result)
        {
            if (result == null)
                return "null";

            var s = new StringBuilder();

            s.Append($"[{result.Length}] ");

            foreach(var r in result)
            {
                s.Append(r.Dump());
            }

            return s.ToString();
        }

        void CheckAst(string input, IAst[] result, string[] expected)
        {
            if (null == expected)
            {
                Assert.True(result == null, $"'{input}': Expected empty parse, got : '{DumpAst(result)}'");
            }
            else
            {
                Assert.True(result.Length == expected.Length, $"'{input}': Expected {result.Length} items, got '{DumpAst(result)}'");
                for (int a = 0; a < result.Length;a++)
                {
                    var res = result[a].Dump();
                    Assert.True(res == expected[a], $"'{input}': Expected '{expected[a]}' but got '{res}'");
                }
            }
        }



    }
}
