using Superpower;
using Superpower.Display;
using Superpower.Model;
using Superpower.Tokenizers;
using System.Collections.Generic;
using System.Net.NetworkInformation;

namespace Humphrey.FrontEnd
{
    public enum Tokens
    {
        None,

        [Token(Category ="Identifier")]
        Identifier,

        [Token(Category = "Keyword")]
        KW_Bit,

        [Token(Category = "Keyword")]
        KW_Return,

        [Token(Category = "Operator", Example = "+")]
        O_Plus,

        [Token(Category = "Syntax", Example =";")]
        S_SemiColon,

        [Token(Category = "Syntax", Example ="{")]
        S_OpenCurlyBrace,

        [Token(Category = "Syntax", Example ="}")]
        S_CloseCurlyBrace,

        [Token(Category = "Syntax", Example ="(")]
        S_OpenParanthesis,

        [Token(Category = "Syntax", Example =")")]
        S_CloseParanthesis,

        [Token(Category = "Syntax", Example =",")]
        S_Comma,

        [Token(Category = "Comment")]
        SingleComment
    }

    public class HumphreyTokeniser : Tokenizer<Tokens>
    {
        readonly Dictionary<char, Tokens> _operators = new Dictionary<char, Tokens>
        {
            ['+'] = Tokens.O_Plus,
            [';'] = Tokens.S_SemiColon,
            [','] = Tokens.S_Comma,
            ['{'] = Tokens.S_OpenCurlyBrace,
            ['}'] = Tokens.S_CloseCurlyBrace,
            ['('] = Tokens.S_OpenParanthesis,
            [')'] = Tokens.S_CloseParanthesis,
        };

        readonly Dictionary<string, Tokens> _keywords = new Dictionary<string, Tokens>
        {
            ["bit"] = Tokens.KW_Bit,
            ["return"] = Tokens.KW_Return,
        };

        protected static Result<char> SkipToNewLine(TextSpan span)
        {
            var next = span.ConsumeChar();
            while (next.HasValue && next.Value != '\n' && next.Value != '\r') 
            {
                next = next.Remainder.ConsumeChar();
            }
            return next;
        }

        protected static Result<char> SkipToNotLetterDigitOrUnderscore(TextSpan span)
        {
            var next = span.ConsumeChar();
            while (next.HasValue && (char.IsLetterOrDigit(next.Value) || next.Value == '_'))
            {
                next = next.Remainder.ConsumeChar();
            }
            return next;
        }

        protected override IEnumerable<Result<Tokens>> Tokenize(TextSpan span)
        {
            var next = SkipWhiteSpace(span);
            if (!next.HasValue)
                yield break;

            do
            {
                var c = next.Value;
                if (_operators.TryGetValue(c, out var token))
                {
                    yield return Result.Value(token, next.Location, next.Remainder);
                    next = next.Remainder.ConsumeChar();
                }
                else if (c == '#')
                {
                    var start = next.Location;
                    next = SkipToNewLine(start);
                    yield return Result.Value(Tokens.SingleComment, start, next.Location);
                }
                else if (char.IsLetter(c) || c == '_')
                {
                    var start = next.Location;
                    next = SkipToNotLetterDigitOrUnderscore(start);
                    var keywordCheck = start.Until(next.Location).ToStringValue();
                    if (_keywords.TryGetValue(keywordCheck, out var keyword))
                        yield return Result.Value(keyword, start, next.Location);
                    else
                        yield return Result.Value(Tokens.Identifier, start, next.Location);
                }
                else
                {
                    yield return Result.Empty<Tokens>(next.Location, new[] { "?" });
                }

                next = SkipWhiteSpace(next.Location);
            } while (next.HasValue);
        }
    }

}
