using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Humphrey.FrontEnd
{

    [System.AttributeUsage(System.AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    public class TokenAttribute : System.Attribute
    {
        public TokenAttribute()
        {
        }

        public string Category {get;set;}
        public string Example {get;set;}
    }


    public enum Tokens
    {
        None,

        [Token(Category ="Identifier", Example = "variable_name")]
        Identifier,

        [Token(Category = "Number", Example = "1234")]
        Number, 

        [Token(Category = "Keyword")]
        KW_Bit,

        [Token(Category = "Keyword")]
        KW_Return,

        [Token(Category = "Operator", Example = "+")]
        O_Plus,

        [Token(Category = "Operator", Example = "-")]
        O_Subtract,

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

        [Token(Category = "Syntax", Example ="_")]
        S_Underscore,

        [Token(Category = "Comment")]
        SingleComment,

        [Token(Category = "Comment")]
        MultiLineComment
    }

    public struct TokenSpan
    {
        public TokenSpan(string e, int p)
        {
            encompass = e;
            position = p;
        }

        string encompass;
        int position;

        public Result<char> ConsumeChar()
        {
            if (AtEnd)
                return new Result<char>(this);

            return new Result<char>(encompass[position], this, new TokenSpan(encompass, ++position));
        }

        public string ToStringValue(TokenSpan remain)
        {
            return encompass.Substring(position, remain.position - position);
        }

        public bool AtEnd => position >= encompass.Length;
        public Result<char> Until(TokenSpan location)
        {
            return new Result<char>(encompass[position], this, location);
        }
    }

    public struct Result<T>
    {
        public Result(T val, TokenSpan loc, TokenSpan remain)
        {
            value = val;
            location = loc;
            remaining = remain;
            hasValue = true;
        }

        public Result(TokenSpan remain)
        {
            hasValue = false;
            location = remain;
            remaining = remain;
            value = default;
        }

        TokenSpan remaining;
        TokenSpan location;
        readonly T value;
        bool hasValue;

        public string ToStringValue()
        {
            return location.ToStringValue(remaining);
        }

        public bool HasValue => hasValue;
        public T Value => value;
        public TokenSpan Location => location;
        public TokenSpan Remainder => remaining;
    }

    public class HumphreyTokeniser 
    {
        readonly Dictionary<char, Tokens> _operators = new Dictionary<char, Tokens>
        {
            ['+'] = Tokens.O_Plus,
            ['-'] = Tokens.O_Subtract,
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

        protected static Result<char> SkipWhiteSpace(TokenSpan span)
        {
            var next = span.ConsumeChar();
            while (next.HasValue && char.IsWhiteSpace(next.Value))
            {
                next = next.Remainder.ConsumeChar();
            }
            return next;
        }

        protected static Result<char> SkipToNewLine(TokenSpan span)
        {
            var next = span.ConsumeChar();
            while (next.HasValue && next.Value != '\n' && next.Value != '\r') 
            {
                next = next.Remainder.ConsumeChar();
            }
            return next;
        }

        protected static bool IsNumberOrIdentifierStart(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_' || c == '$' || c == '%';
        }

        protected static Result<char> SkipToNotNumberOrIdentifier(char firstChar, TokenSpan span, out Tokens kind)
        {
            bool operatorOnly = firstChar == '_';
            bool digitOnly = char.IsDigit(firstChar);
            var endNumber = 0;

            var next = span.ConsumeChar();
            while (next.HasValue)
            {
                var c = next.Value;
                if (endNumber>0)
                {
                    if (endNumber==1)
                    {
                        if (!(c >= '₀' && c <= '₉'))
                            break;
                    }
                    else if (endNumber==2)
                    {
                        if (c != '_')
                            break;
                        endNumber = 3;
                    }
                    else if (endNumber==3)
                    {
                        if (!char.IsDigit(c))
                            break;
                    }
                }
                else
                {
                    if (firstChar == '$')
                    {
                        if (!(char.IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F') || c=='_'))
                            break;
                    }
                    else if (firstChar == '%')
                    {
                        if (!(c == '0' || c == '1' || c == '_'))
                            break;
                    }
                    else if (c != '_')
                    {
                        operatorOnly = false;

                        if (c >= '₀' && c <= '₉')    // end of a number reached parse radix
                        {
                            endNumber = 1;
                        }
                        else if (c == '\\')
                        {
                            endNumber = 2;
                        }
                        else if (!char.IsDigit(c))
                        {
                            if (!char.IsLetterOrDigit(c))
                                break;
                            digitOnly = false;
                        }
                    }
                }
                next = next.Remainder.ConsumeChar();
            }
            if (operatorOnly)
            {
                kind = Tokens.S_Underscore;
            }
            else if (endNumber>0 || firstChar == '$' || firstChar == '%' || digitOnly)
            {
                kind = Tokens.Number;
            }
            else
            {
                kind = Tokens.Identifier;
            }

            return next;
        }

        protected static Result<char> SkipToEndCommentBlock(TokenSpan span)
        {
            var next = span.ConsumeChar();
            int blockCommentDepth = 1;
            while (next.HasValue && blockCommentDepth > 0)
            {
                if (next.Value == '#')
                {
                    next = next.Remainder.ConsumeChar();
                    if (!next.HasValue)
                        break;
                    if (next.Value == '!')
                        blockCommentDepth++;
                }
                else if (next.Value == '!')
                {
                    next = next.Remainder.ConsumeChar();
                    if (!next.HasValue)
                        break;
                    if (next.Value == '!')
                        blockCommentDepth--;
                }
                next = next.Remainder.ConsumeChar();
            }
            return next;
        }

        protected static string Decimalise(string v, int radix)
        {
            BigInteger bigNumber = 0;
            const string Digits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            if (radix < 2 || radix > Digits.Length)
                return null;
            foreach (var c in v)
            {
                var idx = Digits.IndexOf(char.ToUpper(c));
                if (idx == -1)
                    return null;
                bigNumber *= radix;
                bigNumber += idx;
            }

            return bigNumber.ToString();
        }

        // Note should only be called if number detected during tokenisation
        public static string ConvertNumber(string number)
        {
            int offset = 0;
            var builderN = new StringBuilder();
            var builderR = new StringBuilder();

            if (string.IsNullOrEmpty(number))
                return null;
            if (number[0] == '%' || number[0] == '$')
                offset = 1;

            int radix = 0;
            for (int c=offset;c<number.Length;c++)
            {
                if (number[c] == '_')
                    continue;
                else if (radix == 1)
                    builderR.Append(number[c] - '₀');
                else if (radix == 2)
                    radix++;
                else if (radix == 3)
                    builderR.Append(number[c]);
                else
                {
                    if (number[c] >= '₀' && number[c] <= '₉')
                    {
                        builderR.Append(number[c] - '₀');
                        radix = 1;
                    }
                    else if (number[c] == '\\')
                        radix = 2;
                    else 
                        builderN.Append(number[c]);
                }
            }

            if (offset==1 || radix>0)
            {
                int radii = 0;
                if (offset == 1)
                {
                    if (number[0] == '%')
                        radii = 2;
                    else
                        radii = 16;
                }
                else
                    radii = int.Parse(builderR.ToString());

                return Decimalise(builderN.ToString(), radii);
            }

            return builderN.ToString();
        }

        protected bool IsLegalNumberFormat(string number)
        {
            return ConvertNumber(number) != null;
        }

        public IEnumerable<Result<Tokens>> Tokenize(string input)
        {
            return Tokenize(new TokenSpan(input, 0));
        }

        protected IEnumerable<Result<Tokens>> Tokenize(TokenSpan span)
        {
            var next = SkipWhiteSpace(span);
            if (!next.HasValue)
                yield break;

            do
            {
                var c = next.Value;
                if (_operators.TryGetValue(c, out var token))
                {
                    yield return new Result<Tokens>(token, next.Location, next.Remainder);
                    next = next.Remainder.ConsumeChar();
                }
                else if (c == '#')
                {
                    var start = next.Location;
                    next = next.Remainder.ConsumeChar();
                    if (!next.HasValue)
                        yield return new Result<Tokens>(Tokens.SingleComment, start, next.Remainder);
                    else
                    {
                        c = next.Value;
                        if (c == '!')
                        {
                            next = SkipToEndCommentBlock(start);
                            yield return new Result<Tokens>(Tokens.MultiLineComment, start, next.Location);
                        }
                        else
                        {
                            next = SkipToNewLine(start);
                            yield return new Result<Tokens>(Tokens.SingleComment, start, next.Location);
                        }
                    }
                }
                else if (IsNumberOrIdentifierStart(c))
                {
                    var start = next;
                    next = next.Remainder.ConsumeChar();
                    next = SkipToNotNumberOrIdentifier(c, next.Location, out var kind);
                    var keywordCheck = start.Location.Until(next.Location).ToStringValue();

                    if (kind == Tokens.Identifier)
                    {
                        if (_keywords.TryGetValue(keywordCheck, out var keyword))
                            yield return new Result<Tokens>(keyword, start.Location, next.Location);
                        else
                            yield return new Result<Tokens>(Tokens.Identifier, start.Location, next.Location);
                    }
                    else if (kind == Tokens.Number)
                    {
                        if (IsLegalNumberFormat(keywordCheck))
                            yield return new Result<Tokens>(Tokens.Number, start.Location, next.Location);
                        else
                            yield break;
                    }
                    else
                    {
                        // All the characters are underscore, for now return them as operators
                        foreach (var _ in keywordCheck)
                        {
                            yield return new Result<Tokens>(Tokens.S_Underscore, start.Location, start.Remainder);
                            start = start.Remainder.ConsumeChar();
                        }
                    }
                }
                else
                {
                    yield return new Result<Tokens>();
                }

                next = SkipWhiteSpace(next.Location);
            } while (next.HasValue);
        }
    }

}
