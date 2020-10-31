using Humphrey.Backend;
using Humphrey.FrontEnd;
using System.Linq;
using Xunit;

namespace Humphrey.Tests.src
{
    public class MessageTests
    {
        [Theory]
        [InlineData("#", CompilerErrorKind.Debug)]
        [InlineData("\b", CompilerErrorKind.Error_InvalidToken)]
        public void CheckComments(string input, CompilerErrorKind kind)
        {
            TokenTest(input, kind);
        }

        private void TokenTest(string input, CompilerErrorKind expected)
        {
            var messages = new CompilerMessages(false, true, false);
            var tokenise = new HumphreyTokeniser(messages);
            var tokens = tokenise.Tokenize(input);
            var list = tokens.ToList();
            if (expected == CompilerErrorKind.Debug)
                Assert.True(messages.Dump().Length == 0, $"No compiler messages should have been generated but got {messages.Dump()}");
            else
                Assert.True(messages.HasMessageKindBeenLogged(expected), $"Expected message code {(uint)expected:D4} but was not found");
        }
        
        [Theory]
        [InlineData("WorkingFunction:()(out:bit)={out=0;}", CompilerErrorKind.Debug)]
        [InlineData("WorkingFunction:()(out1:bit,out2:bit)={out1,out2=0;}", CompilerErrorKind.Debug)]
        [InlineData("BrokenFunction:()(out:bit)={}", CompilerErrorKind.Error_MissingOutputAssignment)]
        [InlineData("BrokenFunction:()(out1:bit,out2:bit)={out1=0;}", CompilerErrorKind.Error_MissingOutputAssignment)]
        [InlineData("BrokenFunction:()(out1:bit,out2:bit)={out2=0;}", CompilerErrorKind.Error_MissingOutputAssignment)]
        [InlineData("BrokenFunction:()(out1:bit,out2:bit)={}", CompilerErrorKind.Error_MissingOutputAssignment)]
        [InlineData("MismatchSize:()(out:[4]bit)={out=99;}", CompilerErrorKind.Error_IntegerWidthMismatch)]
        [InlineData("byte:[8]bit", CompilerErrorKind.Debug)]
        [InlineData("byte:bit[8]", CompilerErrorKind.Error_ExpectedGlobalDefinition)]
        [InlineData("okStruct:{byte:bit}", CompilerErrorKind.Debug)]
        [InlineData("brokenStruct:{byte:bit[}", CompilerErrorKind.Error_ExpectedStructMemberDefinition)]
        [InlineData("okEnum:bit{True:=1}", CompilerErrorKind.Debug)]
        [InlineData("brokenEnum:bit{True=1}", CompilerErrorKind.Error_ExpectedEnumMemberDefinition)]
        [InlineData("myType:bit bob:myType", CompilerErrorKind.Debug)]
        [InlineData("brokenType:bit bob:myType", CompilerErrorKind.Error_UndefinedType)]
        [InlineData("Func:()()={bob,cat:bit=1; cat=bob;}", CompilerErrorKind.Debug)]
        [InlineData("Func:()()={brokenValue,cat:bit=1; cat=bob;}", CompilerErrorKind.Error_UndefinedValue)]
        [InlineData("ptr:*bit=1 as *bit", CompilerErrorKind.Debug)]
        [InlineData("ptr:*bit=1", CompilerErrorKind.Error_TypeMismatch)]
        [InlineData("ptr:*bit=1 as *[8]bit", CompilerErrorKind.Error_TypeMismatch)]
        public void CheckCompilationMessages(string input, CompilerErrorKind kind)
        {
            CompilationTest(input, kind);
        }

        private void CompilationTest(string input, CompilerErrorKind expected)
        {
            var messages = new CompilerMessages(true, true, false);
            var tokenise = new HumphreyTokeniser(messages);
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens, messages);
            var parsed = parser.File();
            var unit = new HumphreyCompiler(messages).Compile(parsed, "test", "x86_64", false, false);
            if (expected == CompilerErrorKind.Debug)
                Assert.True(messages.Dump().Length == 0, $"No compiler messages should have been generated but got {messages.Dump()}");
            else
                Assert.True(messages.HasMessageKindBeenLogged(expected), $"Expected message code {(uint)expected:D4} ({expected}) but was not found");
        }
    }
}

