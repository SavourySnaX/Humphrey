using Humphrey.FrontEnd;
using System.Linq;
using Xunit;

namespace Humphrey.Tests
{
    public class MessageTests
    {
        [Theory]
        [InlineData("#", CompilerErrorKind.Debug)]
        [InlineData("\b", CompilerErrorKind.Error_InvalidToken)]
        [InlineData("\"", CompilerErrorKind.Error_FailedToFindEndOfString)]
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
        [InlineData("dupMemberStruct:{byte:bit byte:bit}", CompilerErrorKind.Error_DuplicateSymbol)]
        [InlineData("okMemberStruct:{_:bit _:bit}", CompilerErrorKind.Debug)]
        [InlineData("brokenStruct:{byte:bit[}", CompilerErrorKind.Error_ExpectedStructMemberDefinition)]
        [InlineData("okEnum:bit{True:=1}", CompilerErrorKind.Debug)]
        [InlineData("dupMemberEnum:bit{True:=1 True:=0}", CompilerErrorKind.Error_DuplicateSymbol)]
        [InlineData("brokenEnum:bit{True=1}", CompilerErrorKind.Error_ExpectedEnumMemberDefinition)]
        [InlineData("okAlias:bit|{bool:bit}", CompilerErrorKind.Debug)]
        [InlineData("dupMemberAlias:bit|{bool:bit bool:bit}", CompilerErrorKind.Error_DuplicateSymbol)]
        [InlineData("okAlias:[2]bit|{_:bit _:bit}", CompilerErrorKind.Debug)]
        [InlineData("okAlias:[2]bit|{_:bit}", CompilerErrorKind.Error_AliasWidthMismatch)]
        [InlineData("myType:bit bob:myType", CompilerErrorKind.Debug)]
        [InlineData("brokenType:bit bob:myType", CompilerErrorKind.Error_UndefinedType)]
        [InlineData("brokenType:=_", CompilerErrorKind.Error_UndefinedType)]
        [InlineData("Func:()()={bob,cat:bit=1; cat=bob;}", CompilerErrorKind.Debug)]
        [InlineData("Func:()()={brokenValue,cat:bit=1; cat=bob;}", CompilerErrorKind.Error_UndefinedValue)]
        [InlineData("Func:()()={} Func:()()={}", CompilerErrorKind.Error_DuplicateSymbol)]
        [InlineData("Func:bit Func:()()={}", CompilerErrorKind.Error_DuplicateSymbol)]
        [InlineData("Func:=_", CompilerErrorKind.Error_UndefinedType)]
        [InlineData("ptr:*bit=1 as *bit", CompilerErrorKind.Debug)]
        [InlineData("ptr:*bit=1", CompilerErrorKind.Error_TypeMismatch)]
        [InlineData("ptr:*bit=1 as *[8]bit", CompilerErrorKind.Error_TypeMismatch)]
        [InlineData("Working:()()={bob:[6][8]bit=0; bob=\"Hello\"; }", CompilerErrorKind.Debug)]
        [InlineData("byte:[8]bit Working:()()={bob:[6]byte=\"Hello\"; }", CompilerErrorKind.Debug)]
        [InlineData("Broken:()()={bob:[8]bit=255; carol:[-8]bit=bob; }", CompilerErrorKind.Error_SignedUnsignedMismatch)]
        [InlineData("Broken:()()={bob:[1][8]bit=\"Hello\"; }", CompilerErrorKind.Error_TypeMismatch)]
        [InlineData("Broken:()()={bob:[20][8]bit=\"Hello\"; }", CompilerErrorKind.Error_TypeMismatch)]
        [InlineData("Broken:()()={bob:[2][8]bit=0; bob=\"Hello\"; }", CompilerErrorKind.Error_TypeMismatch)]
        [InlineData("Broken:()()={bob:[22][8]bit=0; bob=\"Hello\"; }", CompilerErrorKind.Error_TypeMismatch)]
        [InlineData("global:=\"Hello\" Broken:()()={bob:[7][8]bit=global; }", CompilerErrorKind.Error_TypeMismatch)]
        [InlineData("global:=\"Hello\" Broken:()()={bob:[5][8]bit=global; }", CompilerErrorKind.Error_TypeMismatch)]
        [InlineData("Main:(a:bit)()={ a={}; }", CompilerErrorKind.Error_MustBeExpression)]
        [InlineData("[metadata]t:bit", CompilerErrorKind.Debug)]
        [InlineData("[]", CompilerErrorKind.Error_EmptyMetaDataNode)]
        [InlineData("[%]", CompilerErrorKind.Error_ExpectedIdentifierList)]
        [InlineData("[metadata%]", CompilerErrorKind.Error_ExpectedToken)]
        [InlineData("func:(bob:bit,carol:bit)()={} main:()()={func(1);}", CompilerErrorKind.Error_SignatureMismatch)]
        [InlineData("[C_CALLING_CONVENTION] func:(bob:bit,carol:bit)(out:*bit) main:()()={do:=func(1) as **bit;}", CompilerErrorKind.Error_SignatureMismatch)]
        public void CheckCompilationMessages(string input, CompilerErrorKind kind)
        {
            CompilationTest(input, kind);
        }

        [Theory]
        [InlineData("Main:(a:[8]bit,b:[8]bit)(out:[8]bit)={if Func==0 {out=12;} else {out=a;}} Func:(a:[8]bit)(o:bit)={ o=1; }", CompilerErrorKind.Error_TypeMismatch)]
        public void CheckMoreCompilationMessages(string input, CompilerErrorKind kind)
        {
            CompilationTest(input, kind);
        }

        private void CompilationTest(string input, CompilerErrorKind expected)
        {
            var messages = new CompilerMessages(true, true, false);
            var tokenise = new HumphreyTokeniser(messages);
            var tokens = tokenise.Tokenize(input);
            if (!messages.HasErrors)
            {
                var parser = new HumphreyParser(tokens, messages);
                var parsed = parser.File();
                if (!messages.HasErrors)
                {
                    var semantic = new SemanticPass(null, messages);
                    semantic.RunPass(parsed);
                    if (!messages.HasErrors)
                    {
                        var unit = new HumphreyCompiler(messages).Compile(semantic, "test", "x86_64", false, false);
                    }
                }
            }
            if (expected == CompilerErrorKind.Debug)
                Assert.True(messages.Dump().Length == 0, $"No compiler messages should have been generated but got {messages.Dump()}");
            else
                Assert.True(messages.HasMessageKindBeenLogged(expected), $"Expected message code {(uint)expected:D4} ({expected}) but was not found");
        }
    }
}

