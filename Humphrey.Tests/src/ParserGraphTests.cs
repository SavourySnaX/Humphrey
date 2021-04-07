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
        [InlineData("Main:()()={ partial.lib }", "lib", CompilerErrorKind.Error_ExpectedToken, new [] {typeof(AstGlobalDefinition),typeof(AstCodeBlock),typeof(AstExpressionStatement), typeof(AstBinaryReference), typeof(AstLoadableIdentifier), typeof(AstIdentifier)})]
        [InlineData("Main:()()={ partial.lib= }", null, CompilerErrorKind.Error_MustBeExpression, new [] {typeof(AstGlobalDefinition),typeof(AstCodeBlock), typeof(AstAssignmentStatement), typeof(AstExpressionList), typeof(AstBinaryReference), typeof(AstLoadableIdentifier), typeof(AstIdentifier)})]
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
                    if (symbol != null)
                    {
                        if (t is IIdentifier identifier)
                        {
                            Assert.True(identifier.Name == symbol);
                        }
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
                        foreach (var s in IterateGraph(astBinaryReference.LHS))
                        {
                            pending.Enqueue(s);
                        }
                        pending.Enqueue(astBinaryReference.RHS);
                        break;
                    case AstLoadableIdentifier astLoadableIdentifier:
                        break;
                    case AstIdentifier astIdentifier:
                        break;
                    case AstAssignmentStatement astAssignmentStatement:
                        foreach (var s in IterateGraph(astAssignmentStatement.ExpressionList))
                        {
                            pending.Enqueue(s);
                        }
                        pending.Enqueue(astAssignmentStatement.Assignable);
                        break;
                    case AstExpressionList astExpressionList:
                        foreach (var e in astExpressionList.Expressions)
                        {
                            pending.Enqueue(e);
                        }
                        break;
                    default:
                        throw new System.NotImplementedException($"TODO");
                }
            }
        }
    }
}

