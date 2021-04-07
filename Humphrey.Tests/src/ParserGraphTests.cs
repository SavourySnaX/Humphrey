using System.Collections.Generic;
using Xunit;

namespace Humphrey.FrontEnd.Tests
{
    public class ParserGraphTests
    {

        [Theory]
        [InlineData("Main:()()={ partial; }", "partial", CompilerErrorKind.Debug, new [] {typeof(AstGlobalDefinition),typeof(AstCodeBlock),typeof(AstExpressionStatement), typeof(AstLoadableIdentifier)})]
        [InlineData("Main:()()={ partial }", "partial", CompilerErrorKind.Error_ExpectedToken, new [] {typeof(AstGlobalDefinition),typeof(AstCodeBlock),typeof(AstExpressionStatement), typeof(AstLoadableIdentifier)})]
        [InlineData("Main:()()={ partial. }", "partial", CompilerErrorKind.Error_ExpectedIdentifier, new [] {typeof(AstGlobalDefinition),typeof(AstCodeBlock),typeof(AstExpressionStatement), typeof(AstBinaryReference), typeof(AstLoadableIdentifier)})]
        public void PartialRecovery(string input, string symbol, CompilerErrorKind expectedError, System.Type[] types)
        {
            var messages = new CompilerMessages(false, false, false);
            var tokenise = new HumphreyTokeniser(messages);
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens, messages);
            var parsed = parser.File();

            if (expectedError != CompilerErrorKind.Debug)
            {
                Assert.True(messages.HasMessageKindBeenLogged(expectedError));
            }

            Assert.True(parsed.Length==1);  // Only 1 global at once for now

            int compareIdx = 0;
            foreach (var t in IterateGraph(parsed[0]))
            {
                Assert.True(types[compareIdx] == t.GetType(), $"{types[compareIdx]}!={t.GetType()}");
                compareIdx++;
                if (compareIdx==types.Length)
                {
                    if (t is IIdentifier identifier)
                    {
                        Assert.True(identifier.Name == symbol);
                    }
                    break;
                }
            }
        }

        IEnumerable<IAst> IterateGraph(IAst root)
        {
            Queue<IAst> pending = new Queue<IAst>();

            pending.Enqueue(root);

            while (pending.Count>0)
            {
                var next = pending.Dequeue();
                yield return next;

                switch (next)
                {
                    case AstGlobalDefinition astGlobalDefinition:

                        pending.Enqueue(astGlobalDefinition.Initialiser);
                        break;
                    case AstCodeBlock astCodeBlock:
                        foreach (var s in astCodeBlock.Statements)
                        {
                            pending.Enqueue(s);
                        }
                        break;
                    case AstExpressionStatement astExpressionStatement:
                        pending.Enqueue(astExpressionStatement.Expression);
                        break;
                    case AstBinaryReference astBinaryReference:
                        pending.Enqueue(astBinaryReference.LHS);
                        pending.Enqueue(astBinaryReference.RHS);
                        break;
                    case AstLoadableIdentifier astLoadableIdentifier:
                        break;
                    case AstIdentifier astIdentifier:
                        break;
                    default:
                        throw new System.NotImplementedException($"TODO");
                }
            }
        }
    }
}

