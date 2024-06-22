using System.Text;
using Xunit;

namespace Humphrey.FrontEnd.Tests
{
    public class ParserTests
    {
        [Theory]
        [InlineData("main", new[] { "main" })]
        [InlineData("main_routine", new[] { "main_routine" })]
        [InlineData("main, bob", new[] { "main", "bob" })]
        [InlineData("a,     b, c, d,    e", new[] { "a", "b","c","d","e" })]
        [InlineData("main + bob", new[] { "main" })]
        [InlineData("+", null)]
        [InlineData("return bit", null)]
        [InlineData("_", null)]
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
        [InlineData("1 as bit","as 1 bit")]
        [InlineData("b as c","as b c")]
        [InlineData("b*d as c","as * b d c")]
        [InlineData("01₂+10₂+100₂ as [3]bit","as + + 1 2 4 [3] bit")]
        [InlineData("b.c",". b c")]
        [InlineData("(b).c",". b c")]
        [InlineData("(b).c.d",". . b c d")]
        [InlineData("b.c+a.d","+ . b c . a d")]
        [InlineData("function()", "function ( )")]
        [InlineData("function(5,2)", "function ( 5 , 2 )")]
        [InlineData("function(5+2,2-1)", "function ( + 5 2 , - 2 1 )")]
        [InlineData("function(another(2),other(2-1))", "function ( another ( 2 ) , other ( - 2 1 ) )")]
        [InlineData("a(b).c",". a ( b ) c")]
        [InlineData("a==b","== a b")]
        [InlineData("a<=b","<= a b")]
        [InlineData("a>=b",">= a b")]
        [InlineData("a<b","< a b")]
        [InlineData("a>b","> a b")]
        [InlineData("a!=b","!= a b")]
        [InlineData("!a==b","! == a b")]
        [InlineData("4+5!=6","!= + 4 5 6")]
        [InlineData("array[5]", "array [ 5 ]")]
        [InlineData("arrays[5][1]", "arrays [ 5 ] [ 1 ]")]
        [InlineData("array[15+2]", "array [ + 15 2 ]")]
        [InlineData("array[15+2]+7", "+ array [ + 15 2 ] 7")]
        [InlineData("array[15+2]+getArray()[1]", "+ array [ + 15 2 ] getArray ( ) [ 1 ]")]
        [InlineData("array[getIdx()]", "array [ getIdx ( ) ]")]
        [InlineData("function(array[0], array[1])", "function ( array [ 0 ] , array [ 1 ] )")]
        [InlineData("++a", "++ a")]
        [InlineData("++a/2", "/ ++ a 2")]
        [InlineData("--a", "-- a")]
        [InlineData("3*--a", "* 3 -- a")]
        [InlineData("a++", "(a)++")]
        [InlineData("*a++", "* (a)++")]
        [InlineData("a+++b", "+ (a)++ b")]
        [InlineData("a--", "(a)--")]
        [InlineData("5+a--", "+ 5 (a)--")]
        [InlineData("~a", "~ a")]
        [InlineData("a&b", "& a b")]
        [InlineData("a|b", "| a b")]
        [InlineData("a&&b", "&& a b")]
        [InlineData("a||b", "|| a b")]
        [InlineData("a^b", "^ a b")]
        [InlineData("a==5||b==12", "|| == a 5 == b 12")]
        [InlineData("++a;++a", "++ a")]
        [InlineData("++a;--a", "++ a")]
        [InlineData("--a;++a", "-- a")]
        [InlineData("--a;--a", "-- a")]
        [InlineData("(++a)++", "(++ a)++")]
        [InlineData("this.that++", "(. this that)++")]
        [InlineData("&b","& b")]
        [InlineData("a<<b","<< a b")]
        [InlineData("a>>b",">> a b")]
        [InlineData("a>>>b",">>> a b")]
        [InlineData("a<<b==c","== << a b c")]
        [InlineData("a>>b==c","== >> a b c")]
        [InlineData("a>>>b==c","== >>> a b c")]
        [InlineData("()",null)]
        [InlineData("[]",null)]
        [InlineData("(]",null)]
        public void CheckExpression(string input, string expected)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens);
            CheckAst(input, parser.ParseExpression(), expected);
        }

        [Theory]
        [InlineData("*wat.the++","* (. wat the)++")]
        [InlineData("*(wat.the)++","* (. wat the)++")]
        [InlineData("*wat.the", "* . wat the")]
        [InlineData("wat.the.h", ". . wat the h")]
        public void CheckExpressionExtra(string input, string expected)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens);
            CheckAst(input, parser.ParseExpression(), expected);
        }
        
        [Theory]
        [InlineData("System::Types::wat", "System::Types::wat")]
        [InlineData("System::Types::wat+bob", "+ System::Types::wat bob")]
        public void CheckExpressionNamespace(string input, string expected)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens);
            CheckAst(input, parser.ParseExpression(), expected);
        }

        [Theory]
        [InlineData("using System", "using System")]
        [InlineData("using System::Types", "using System::Types")]
        [InlineData("using System::Types as Goldfish", "using System::Types as Goldfish")]
        public void CheckUsingNamespace(string input, string expected)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens);
            CheckAst(input, parser.Using(), expected);
        }


        [Theory]
        [InlineData("\"blah\"", "\"blah\"")]
        [InlineData("\"\"", "\"\"")]
        [InlineData("\"執筆\"", "\"執筆\"")]
        [InlineData("\"執筆\"\\_32", "\"執筆\"")]
        [InlineData("\"執筆\"\\_16", "\"執筆\"")]
        [InlineData("\"執筆\"\\_8", "\"執筆\"")]
        [InlineData("\"執筆\"₈", "\"執筆\"")]
        [InlineData("\"\\0\\a\\b\\e\\f\\r\\n\\t\\v\\'\\\"\\\\\"", "\"\0\a\b\x1b\f\r\n\t\v\'\"\\\"")]
        [InlineData("\"",null)]
        public void CheckExpressionString(string input, string expected)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens);
            CheckAst(input, parser.ParseExpression(), expected);
        }


        [Theory]
        [InlineData("a=1","a = 1")]
        [InlineData("a[15]=1","a [ 15 ] = 1")]
        [InlineData("a()=1","a ( ) = 1")]
        [InlineData("a[15]()=1","a [ 15 ] ( ) = 1")]
        [InlineData("a,x,y=1","a , x , y = 1")]
        [InlineData("a,x,y=function()","a , x , y = function ( )")]
        public void CheckAssignment(string input, string expected)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens);
            CheckAst(input, parser.Assignment(), expected);
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
        [InlineData("fp32", "fp32")]
        public void CheckType(string input, string expected)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens);
            CheckAst(input, parser.BaseType(), expected);
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
        [InlineData("bitval:=1", "bitval := 1")]
        [InlineData("bitval:=1*0", "bitval := * 1 0")]
        [InlineData("bitval,other:=1*0", "bitval , other := * 1 0")]
        [InlineData("bitval:=a", "bitval := a")]
        [InlineData("a=bit", null)]
        [InlineData("a=[1] bit", null)]
        [InlineData("FunctionPtr:()()=0", "FunctionPtr : () () = 0")]
        [InlineData("bit:()()=0", null)]
        [InlineData("Main:()(returnVal:bit)", "Main : () (returnVal : bit)")]
        [InlineData("Main,ReturnsBit:()(returnVal:bit)", "Main , ReturnsBit : () (returnVal : bit)")]
        [InlineData("AliasKind:[8]bit|{_:[7]bit lsb:[1]bit}=0", "AliasKind : [8] bit |{ _ : [7] bit lsb : [1] bit} = 0")]
        public void CheckGlobalDefinition(string input, string expected)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens);
            CheckAst(input, parser.GlobalScopeDefinition(), expected);
        }

        [Theory]
        [InlineData("a:bit", "a : bit")]
        [InlineData("a:a", "a : a")]             // Semantically incorrect
        [InlineData("bitval   :bit= 1", "bitval : bit = 1")]
        [InlineData("bitval   :[1]bit= 1", "bitval : [1] bit = 1")]
        [InlineData("bitval:=1", "bitval := 1")]
        [InlineData("bitval:=1*0", "bitval := * 1 0")]
        [InlineData("bitval,other:=1*0", "bitval , other := * 1 0")]
        [InlineData("bitval:=a", "bitval := a")]
        [InlineData("a=bit", null)]
        [InlineData("a=[1] bit", null)]
        [InlineData("FunctionPtr:()()=0", "FunctionPtr : () () = 0")]
        [InlineData("bit:()()=0", null)]
        [InlineData("Main:()(returnVal:bit)", "Main : () (returnVal : bit)")]
        [InlineData("Main,ReturnsBit:()(returnVal:bit)", "Main , ReturnsBit : () (returnVal : bit)")]
        public void CheckLocalDefinition(string input, string expected)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens);
            CheckAst(input, parser.LocalScopeDefinition(), expected);
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
        [InlineData("(a:_)(b:[64]bit)","(a : _) (b : [64] bit)")]
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
        [InlineData("{bob,carol:bit}","{ bob , carol : bit}")]
        [InlineData("{bob:bit squee:[8]bit}","{ bob : bit squee : [8] bit}")]
        [InlineData("{bob,carol:bit squee,bees:[8]bit}","{ bob , carol : bit squee , bees : [8] bit}")]
        [InlineData("{bob:apple}","{ bob : apple}")]
        [InlineData("{_:apple}","{ _ : apple}")]
        [InlineData("{bob:()()}", null)]
        public void CheckStructType(string input, string expected)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens);
            CheckAst(input, parser.StructType(), expected);
        }

        [Theory]
        [InlineData("{}",null)]
        [InlineData("bit{}","bit { }")]
        [InlineData("bit{False:=0 True:=1}","bit { False := 0 True := 1}")]
        [InlineData("bit{False:=0 True:=!False}","bit { False := 0 True := ! False}")]
        [InlineData("[32]bit{Red:=0xFF000000 Green:=0x00FF0000 Blue:=0x0000FF00}","[32] bit { Red := 4278190080 Green := 16711680 Blue := 65280}")]
        public void CheckEnumType(string input, string expected)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens);
            CheckAst(input, parser.EnumType(parser.BaseType()), expected);
        }

        [Theory]
        [InlineData("|{}", null)]
        [InlineData("bit|{}", "bit |{ }")]
        [InlineData("a |{ a:b }", "a |{ a : b}")]
        [InlineData("a |{ a:b }|{c:b}", "a |{ a : b} |{ c : b}")]
        [InlineData("[32]bit |{ MSB:[8]bit _:[16]bit LSB:[8]bit }", "[32] bit |{ MSB : [8] bit _ : [16] bit LSB : [8] bit}")]
        public void CheckAliasType(string input, string expected)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens);
            CheckAst(input, parser.AliasType(parser.BaseType()), expected);
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
            CheckAst(input, parser.ExpressionList()?.Expressions, expected);
        }

        [Theory]
        [InlineData("{}","{ }")]
        [InlineData("{return;}","{ return}")]
        [InlineData("return;", "return")]
        [InlineData("a:bit=1;", "a : bit = 1")]
        [InlineData("a=1;", "a = 1")]
        [InlineData("for x = 0..1 {}", "for x = 0 .. 1 { }")]
        [InlineData("for x = 0..1", null)]
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
        public void CheckCodeBlock(string input, string expected)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens);
            CheckAst(input, parser.CodeBlock(), expected);
        }

        [Theory]
        [InlineData("[]", null)]
        [InlineData("[bob]", "[ bob ]")]
        [InlineData("[bob,carol]","[ bob , carol ]")]
        public void CheckMeta(string input, string expected)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens);
            CheckAst(input, parser.MetaDataNode(), expected);
        }


        [Theory]
        [InlineData("()", "")]
        [InlineData("(5)", "5")]
        [InlineData("(5,2)", "5 , 2")]
        [InlineData("(5+3,2-1)", "+ 5 3 , - 2 1")]
        [InlineData("((5+3),(2-1))", "+ 5 3 , - 2 1")]
        [InlineData("(a(5+3),b(2-1),0)", "a ( + 5 3 ) , b ( - 2 1 ) , 0")]
        [InlineData("(a(5+3),b(2-1),c)", "a ( + 5 3 ) , b ( - 2 1 ) , c")]
        [InlineData("(())", null)]
        public void CheckFunctionArguments(string input, string expected)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens);
            CheckAst(input, CheckFunctionArgumentsHelper(parser), expected);
        }

        [Theory]
        [InlineData("0..1", "0 .. 1")]
        [InlineData("b..x", "b .. x")]
        [InlineData("0+0..5*2", "+ 0 0 .. * 5 2")]
        [InlineData("0+0..9*a", "+ 0 0 .. * 9 a")]
        [InlineData("..", null)]
        public void CheckRange(string input, string expected)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens);
            CheckAst(input, parser.Range(), expected);
        }

        [Theory]
        [InlineData("for x = 0..1 {}", "for x = 0 .. 1 { }")]
        public void CheckForStatement(string input, string expected)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens);
            CheckAst(input, parser.ForStatement(), expected);
        }
        
        [Theory]
        [InlineData("if x == 0 {}", "if == x 0 { }")]
        [InlineData("if x == 0 {} else {}", "if == x 0 { } else { }")]
        [InlineData("if x == 0 {if b{}} else {}", "if == x 0 { if b { }} else { }")]
        public void CheckIfStatement(string input, string expected)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens);
            CheckAst(input, parser.IfStatement(), expected);
        }

        [Theory]
        [InlineData("while x == 0 {}", "while == x 0 { }")]
        [InlineData("while x == 0 return", null)]
        public void CheckWhileStatement(string input, string expected)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens);
            CheckAst(input, parser.WhileStatement(), expected);
        }

        [Theory]
        [InlineData("[]", null)]
        [InlineData("[5]", "5")]
        [InlineData("[5,2]", null)]
        [InlineData("[5+3]", "+ 5 3")]
        [InlineData("[(5+3)]", "+ 5 3")]
        [InlineData("[getIdx()]", "getIdx ( )")]
        [InlineData("[0..5]", "0 .. 5")]
        [InlineData("[..5]", ".. 5")]
        [InlineData("[0..]", "0 ..")]
        [InlineData("[5+3..9*2]", "+ 5 3 .. * 9 2")]
        [InlineData("[()]", null)]
        [InlineData("[..]", null)]
        public void CheckArraySubscript(string input, string expected)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens);
            CheckAst(input, CheckArraySubscriptHelper(parser), expected);
        }

        IAst CheckArraySubscriptHelper(HumphreyParser parser)
        {
            var oper = parser.ArraySubscriptOperator() as IOperator;
            if (oper != null)
            {
                return parser.ArraySubscript(oper);
            }
            return null;
        }
        IAst CheckFunctionArgumentsHelper(HumphreyParser parser)
        {
            if (parser.OpenParanthesis())
            {
                return parser.FunctionCallArguments();
            }
            return null;
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
