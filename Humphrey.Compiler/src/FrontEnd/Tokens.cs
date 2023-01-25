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

        public string Category { get; set; }
        public string Example { get; set; }

        public string SemanticKind { get; set; }
    }

    public enum Tokens
    {
        None,

        [Token(Category = "Identifier", Example = "variable_name", SemanticKind = "Variable|Type|Struct|Enum|Parameter|EnumValue")]
        Identifier,

        [Token(Category = "Number", Example = "1234", SemanticKind = "Number")]
        Number,

        [Token(Category = "Keyword", Example = "bit", SemanticKind = "Type")]
        KW_Bit,

        [Token(Category = "Keyword", Example = "using", SemanticKind = "Keyword")]
        KW_Using,

        [Token(Category = "Keyword", Example = "for", SemanticKind = "Keyword")]
        KW_For,

        [Token(Category = "Keyword", Example = "if", SemanticKind = "Keyword")]
        KW_If,

        [Token(Category = "Keyword", Example = "else", SemanticKind = "Keyword")]
        KW_Else,

        [Token(Category = "Keyword", Example = "return", SemanticKind = "Keyword")]
        KW_Return,
        
        [Token(Category = "Keyword", Example = "while", SemanticKind = "Keyword")]
        KW_While,

        [Token(Category = "Operator", Example = "+", SemanticKind = "Operator")]
        O_Plus,

        [Token(Category = "Operator", Example = "-", SemanticKind = "Operator")]
        O_Subtract,

        [Token(Category = "Operator", Example = "*", SemanticKind = "Operator")]
        O_Multiply,

        [Token(Category = "Operator", Example = "/", SemanticKind = "Operator")]
        O_Divide,

        [Token(Category = "Operator", Example = "%", SemanticKind = "Operator")]
        O_Modulus,

        [Token(Category = "Operator", Example = "=", SemanticKind = "Operator")]
        O_Equals,

        [Token(Category = "Operator", Example = "!", SemanticKind = "Operator")]
        O_LogicalNot,
        
        [Token(Category = "Operator", Example = "||", SemanticKind = "Operator")]
        O_LogicalOr,
        
        [Token(Category = "Operator", Example = "&&", SemanticKind = "Operator")]
        O_LogicalAnd,

        [Token(Category = "Operator", Example = "~", SemanticKind = "Operator")]
        O_BinaryNot,
        
        [Token(Category = "Operator", Example = "|", SemanticKind = "Operator")]
        O_BinaryOr,
        
        [Token(Category = "Operator", Example = "&", SemanticKind = "Operator")]
        O_BinaryAnd,

        [Token(Category = "Operator", Example = "^", SemanticKind = "Operator")]
        O_BinaryXor,

        [Token(Category = "Operator", Example = ".", SemanticKind = "Operator")]
        O_Dot,
        [Token(Category = "Operator", Example = "..", SemanticKind = "Operator")]
        O_DotDot,

        [Token(Category = "Operator", Example = ":", SemanticKind = "Operator")]
        O_Colon,
        
        [Token(Category = "Operator", Example = "==", SemanticKind = "Operator")]
        O_EqualsEquals,

        [Token(Category = "Operator", Example = "++", SemanticKind = "Operator")]
        O_PlusPlus,

        [Token(Category = "Operator", Example = "--", SemanticKind = "Operator")]
        O_MinusMinus,

        [Token(Category = "Operator", Example = "!=", SemanticKind = "Operator")]
        O_NotEquals,

        [Token(Category = "Operator", Example = "<=", SemanticKind = "Operator")]
        O_LessEquals,

        [Token(Category = "Operator", Example = ">=", SemanticKind = "Operator")]
        O_GreaterEquals,
        
        [Token(Category = "Operator", Example = "<", SemanticKind = "Operator")]
        O_Less,
        
        [Token(Category = "Operator", Example = ">", SemanticKind = "Operator")]
        O_Greater,

        [Token(Category = "Operator", Example = "<<", SemanticKind = "Operator")]
        O_LogicalShiftLeft,
        
        [Token(Category = "Operator", Example = ">>", SemanticKind = "Operator")]
        O_LogicalShiftRight,

        [Token(Category = "Operator", Example = ">>>", SemanticKind = "Operator")]
        O_ArithmaticShiftRight,

        [Token(Category = "Operator", Example = "as", SemanticKind = "Keyword")]
        O_As,

        [Token(Category = "Syntax", Example = ";")]
        S_SemiColon,

        [Token(Category = "Syntax", Example = "|{")]
        S_OpenAlias,

        [Token(Category = "Syntax", Example = "{")]
        S_OpenCurlyBrace,

        [Token(Category = "Syntax", Example = "}")]
        S_CloseCurlyBrace,

        [Token(Category = "Syntax", Example = "(")]
        S_OpenParanthesis,

        [Token(Category = "Syntax", Example = ")")]
        S_CloseParanthesis,

        [Token(Category = "Syntax", Example = "[")]
        S_OpenSquareBracket,

        [Token(Category = "Syntax", Example = "]")]
        S_CloseSquareBracket,

        [Token(Category = "Syntax", Example = ",")]
        S_Comma,

        [Token(Category = "Syntax", Example = "::")]
        S_ColonColon,

        [Token(Category = "Syntax", Example = "_")]
        S_Underscore,

        [Token(Category = "Literal", Example = "\"blah\"", SemanticKind = "String")]
        String,

        [Token(Category = "Comment", SemanticKind = "Comment")]
        SingleComment,

        [Token(Category = "Comment", SemanticKind = "Comment")]
        MultiLineComment
    }

    public struct TokenSpan : IEquatable<TokenSpan>
    {
        public TokenSpan(string f, string e, int p = 0, uint l = 1, uint c = 1)
        {
            encompass = e;
            position = p;
            line = l;
            column = c;
            filename = f;
        }

        string filename;
        string encompass;
        int position;
        uint column, line;

        public Result<char> ConsumeChar()
        {
            if (AtEnd)
                return new Result<char>(this);

            var newLine = false;
            uint nxtCol = column;
            uint nxtLin = line;
            var consumed = encompass[position];
            if (Char.GetUnicodeCategory(consumed) == System.Globalization.UnicodeCategory.LineSeparator)
                newLine = true;
            else if (consumed == '\r')
                newLine = true;
            else if (consumed == '\n')
            {
                if (position == 0 || encompass[position - 1] != '\r')
                    newLine = true;
                else
                    nxtCol--;
            }

            if (newLine)
            {
                nxtCol = 1;
                nxtLin++;
            }
            else
                nxtCol++;

            return new Result<char>(encompass[position], this, new TokenSpan(filename, encompass, ++position, nxtLin, nxtCol));
        }


        public string ToStringValue(TokenSpan remain)
        {
            if (encompass==null)
                return "";
            if (remain.AtEnd)
                return encompass.Substring(position);
            return encompass.Substring(position, remain.position - position);
        }

        public string FetchDocLine()
        {
            var dupedLocation = this;
            uint myLine = dupedLocation.line;
            while (!dupedLocation.AtEnd)
            {
                var res = dupedLocation.ConsumeChar();
                if (res.Remainder.line!=myLine)
                    break;
                if (!res.HasValue)
                    break;
                if (res.Value=='#')
                {
                    if (!AtEnd)
                    {
                        if (dupedLocation.encompass[dupedLocation.position]=='!')
                            break;
                        int start = dupedLocation.position;
                        int end = start;
                        // At this point we have a valid doc line.. consume until we reach end/newline
                        while (!dupedLocation.AtEnd)
                        {
                            res = dupedLocation.ConsumeChar();
                            if (res.Remainder.line!=myLine || dupedLocation.AtEnd || !res.HasValue)
                            {
                                // return docline
                                if (dupedLocation.AtEnd)
                                    end++;
                                return dupedLocation.encompass.Substring(start, end-start).Trim();
                            }
                            end = dupedLocation.position;
                        }
                    }
                }
            }
            return "";
        }

        public bool AtEnd => position >= encompass.Length;
        public Result<char> Until(TokenSpan location)
        {
            return new Result<char>(encompass[position], this, location);
        }

        public override string ToString()
        {
            return $"\"{filename}\"@{line}:{column}";
        }

        public string DumpContext(TokenSpan? remain)
        {
            var s = new StringBuilder();
            int length = 1;
            if (remain.HasValue)
            {
                length = remain.Value.position - position;
            }
            if (remain.Value.AtEnd)
            {
                return "";
            }

            // scan backwards to beginning of line from current location
            int scan=position;
            while (scan >= 0)
            {
                var c = encompass[scan];
                if (c == '\r' || c == '\n' || Char.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.LineSeparator)
                {
                    scan++;
                    break;
                }
                scan--;
            }
            if (scan < 0)
                scan = 0;

            int end=position;
            while (end < encompass.Length)
            {
                var c = encompass[end];
                if (c == '\r' || c == '\n' || Char.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.LineSeparator)
                {
                    end--;
                    break;
                }
                end++;
            }
            if (end >= encompass.Length)
                end--;

            for (int a = scan; a <= end; a++)
                s.Append(encompass[a]);
            s.AppendLine();
            for (int a = scan; a < position; a++)
            {
                if (Char.IsWhiteSpace(encompass[a]))
                    s.Append(encompass[a]);
                else
                    s.Append(" ");
            }
            for (int a = 0; a < length; a++)
                s.Append("^");
            s.AppendLine();

            return s.ToString();
        }

        public override bool Equals(object obj) => obj is TokenSpan other && Equals(other);

        public bool Equals(TokenSpan other)
        {
            return (Filename == other.filename) &&
                (Position == other.Position);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Filename.GetHashCode(), Position.GetHashCode());
        }

        public uint Line => line;
        public uint Column => column;
        public string Filename => filename;

        public int Position => position;
    }

    public struct Result<T> : IEquatable<Result<T>>
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

        public string FetchDocLine()
        {
            if (location.AtEnd)
                return "";
            return location.FetchDocLine();
        }

        public Result<T> Combine(Result<T> toCombine)
        {
            if (location.Position < toCombine.location.Position)
                return new Result<T>(value, location, toCombine.remaining);
            return new Result<T>(toCombine.value, toCombine.location, remaining);
        }

        public override bool Equals(object obj) => obj is Result<T> other && Equals(other);
        
        public bool Equals(Result<T> other)
        {
            return location.Equals(other.location) && remaining.Equals(other.remaining);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(location.GetHashCode(), remaining.GetHashCode());
        }

        public bool HasValue => hasValue;
        public T Value => value;
        public TokenSpan Location => location;
        public TokenSpan Remainder => remaining;
    }

    public class HumphreyTokeniser
    {
        ICompilerMessages messages;

        public HumphreyTokeniser(ICompilerMessages overrideDefaultMessages = null)
        {
            messages = overrideDefaultMessages;
            if (messages==null)
                messages = new CompilerMessages(true, true, false);
        }

        readonly Dictionary<char, Tokens> _operators = new Dictionary<char, Tokens>
        {
            ['+'] = Tokens.O_Plus,
            ['-'] = Tokens.O_Subtract,
            ['*'] = Tokens.O_Multiply,
            ['/'] = Tokens.O_Divide,
            ['%'] = Tokens.O_Modulus,
            [':'] = Tokens.O_Colon,
            ['.'] = Tokens.O_Dot,
            ['!'] = Tokens.O_LogicalNot,
            ['~'] = Tokens.O_BinaryNot,
            ['&'] = Tokens.O_BinaryAnd,
            ['|'] = Tokens.O_BinaryOr,
            ['^'] = Tokens.O_BinaryXor,
            ['<'] = Tokens.O_Less,
            ['>'] = Tokens.O_Greater,
            ['='] = Tokens.O_Equals,
            [';'] = Tokens.S_SemiColon,
            [','] = Tokens.S_Comma,
            ['{'] = Tokens.S_OpenCurlyBrace,
            ['}'] = Tokens.S_CloseCurlyBrace,
            ['('] = Tokens.S_OpenParanthesis,
            [')'] = Tokens.S_CloseParanthesis,
            ['['] = Tokens.S_OpenSquareBracket,
            [']'] = Tokens.S_CloseSquareBracket,
        };

        readonly Dictionary<(char, char), Tokens> _dualOperators = new Dictionary<(char, char), Tokens>
        {
            [('.', '.')] = Tokens.O_DotDot,
            [(':', ':')] = Tokens.S_ColonColon,
            [('=', '=')] = Tokens.O_EqualsEquals,
            [('+', '+')] = Tokens.O_PlusPlus,
            [('-', '-')] = Tokens.O_MinusMinus,
            [('!', '=')] = Tokens.O_NotEquals,
            [('<', '=')] = Tokens.O_LessEquals,
            [('>', '=')] = Tokens.O_GreaterEquals,
            [('&', '&')] = Tokens.O_LogicalAnd,
            [('|', '|')] = Tokens.O_LogicalOr,
            [('<', '<')] = Tokens.O_LogicalShiftLeft,
            [('>', '>')] = Tokens.O_LogicalShiftRight,
            [('|', '{')] = Tokens.S_OpenAlias
        };

        readonly Dictionary<(char, char, char), Tokens> _tripleOperators = new Dictionary<(char, char, char), Tokens>
        {
            [('>', '>', '>')] = Tokens.O_ArithmaticShiftRight,
        };

        readonly Dictionary<string, Tokens> _keywords = new Dictionary<string, Tokens>
        {
            ["as"] = Tokens.O_As,
            ["bit"] = Tokens.KW_Bit,
            ["for"] = Tokens.KW_For,
            ["return"] = Tokens.KW_Return,
            ["if"] = Tokens.KW_If,
            ["else"] = Tokens.KW_Else,
            ["while"] = Tokens.KW_While,
            ["using"] = Tokens.KW_Using,
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
            while (next.HasValue && next.Value != '\n' && next.Value != '\r' && Char.GetUnicodeCategory(next.Value) != System.Globalization.UnicodeCategory.LineSeparator)
            {
                next = next.Remainder.ConsumeChar();
            }
            return next;
        }

        protected static bool IsNumberOrIdentifierStart(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_';
        }

        protected static Result<char> SkipToNotNumberOrIdentifier(char firstChar, TokenSpan span, out Tokens kind)
        {
            char secondChar = '\0';
            bool operatorOnly = firstChar == '_';
            bool digitOnly = char.IsDigit(firstChar);
            var endNumber = 0;

            var next = span.ConsumeChar();
            while (next.HasValue)
            {
                var c = next.Value;
                if (endNumber > 0)
                {
                    operatorOnly = false;
                    if (endNumber == 1)
                    {
                        if (!(c >= '₀' && c <= '₉'))
                            break;
                    }
                    else if (endNumber == 2)
                    {
                        if (c != '_')
                            break;
                        endNumber = 3;
                    }
                    else if (endNumber == 3)
                    {
                        if (!char.IsDigit(c))
                            break;
                    }
                }
                else
                {
                    if (firstChar == '0' && secondChar == '\0' && (c == 'x' || c == 'b'))
                    {
                        // Handle x/b
                        if (c == 'x')
                            secondChar = 'x';
                        else if (c == 'b')
                            secondChar = 'b';
                    }
                    else if (secondChar == 'x')
                    {
                        if (!(char.IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F') || c == '_'))
                            break;
                    }
                    else if (secondChar == 'b')
                    {
                        if (!(c == '0' || c == '1' || c == '_'))
                            break;
                    }
                    else if (c != '_')
                    {
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
                            operatorOnly = false;
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
            else if (endNumber > 0 || secondChar == 'x' || secondChar == 'b' || digitOnly)
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
                    if (next.Value == '#')
                        blockCommentDepth--;
                }
                next = next.Remainder.ConsumeChar();
            }
            return next;
        }

        protected static Result<char> SkipToEndString(TokenSpan span)
        {
            var next = span.ConsumeChar();
            var state = 0;

            while (next.HasValue)
            {
                if (state == 0)
                {
                    if (next.Value == '\\')
                        state = 20;
                    else if (next.Value == '"')
                    {
                        next = next.Remainder.ConsumeChar();
                        if (next.HasValue)
                        {
                            if (next.Value == '\\')
                                state = 1;
                            else if (next.Value >= '₀' && next.Value <= '₉')
                                state = 10;
                            else
                                break;
                        }
                    }
                }
                else
                {
                    if (state == 1)
                    {
                        if (next.Value == '_')
                            state = 2;
                        else
                            break;
                    }
                    else
                    {
                        if (state == 2)
                        {
                            if (!char.IsDigit(next.Value))
                                break;
                        }
                        if (state == 10)
                        {
                            if (next.Value < '₀' || next.Value > '₉')
                                break;
                        }
                        if (state == 20)
                        {
                            state = 0;
                        }
                    }
                }

                next = next.Remainder.ConsumeChar();
            }
            return next;
        }

        public static bool ConvertString(string input, out string output, out AstString.StringKind kind)
        {
            output = null;
            kind = AstString.StringKind.UTF8;
            if (!input.StartsWith('"'))
                return false;
            // find end quotation
            var endQuoteOffset = -1;
            for (int offset = input.Length - 1; offset >= 0; offset--)
            {
                if (input[offset]=='"')
                {
                    endQuoteOffset = offset;
                    break;
                }
            }
            if (endQuoteOffset<1)
                return false;

            var builder = new StringBuilder();
            int escapeState = 0;
            for (int offset = 1; offset < endQuoteOffset;offset++)
            {
                if (escapeState==0)
                {
                    if (input[offset]=='\\')
                        escapeState = 1;
                    else
                        builder.Append(input[offset]);
                }
                else
                {
                    if (input[offset]=='a')
                        builder.Append('\a');
                    else if (input[offset]=='b')
                        builder.Append('\b');
                    else if (input[offset]=='e')
                        builder.Append('\x1b');
                    else if (input[offset]=='f')
                        builder.Append('\f');
                    else if (input[offset]=='n')
                        builder.Append('\n');
                    else if (input[offset]=='r')
                        builder.Append('\r');
                    else if (input[offset]=='t')
                        builder.Append('\t');
                    else if (input[offset]=='v')
                        builder.Append('\v');
                    else if (input[offset]=='\'')
                        builder.Append('\'');
                    else if (input[offset]=='\"')
                        builder.Append('\"');
                    else if (input[offset]=='\\')
                        builder.Append('\\');
                    else if (input[offset]=='0')
                        builder.Append('\0');
                    else
                    {
                        return false;
                    }
                    escapeState = 0;
                }
            }
            output = builder.ToString();

            if (endQuoteOffset!=input.Length-1)
            {
                if (!ExtractSubValue(input, endQuoteOffset+1, out var result))
                    return false;
                if (result == 8)
                    kind = AstString.StringKind.UTF8;
                else if (result == 16)
                    kind = AstString.StringKind.UTF16;
                else if (result == 32)
                    kind = AstString.StringKind.UTF32;
                else
                    return false;
            }
            return true;
        }

        protected bool VerifyStringLiteral(string encoded)
        {
            return ConvertString(encoded, out _, out _);
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

            if (string.IsNullOrEmpty(number))
                return null;

            if (number.Length >= 2 && (number[1] == 'b' || number[1] == 'x'))
                offset = 2;

            int extractOffset = -1;
            for (int c = offset; c < number.Length; c++)
            {
                if (number[c] == '_')
                    continue;
                if (number[c] >= '₀' && number[c] <= '₉')
                {
                    extractOffset = c;
                    break;
                }
                else if (number[c] == '\\')
                {
                    extractOffset = c;
                    break;
                }
                else
                {
                    builderN.Append(number[c]);
                }
            }

            var radii = 10;
            if (extractOffset != -1)
            {
                if (!ExtractSubValue(number, extractOffset, out radii))
                    return null;
            }

            if (offset == 2)
            {
                if (offset == 2)
                {
                    if (number[1] == 'b')
                        radii = 2;
                    else
                        radii = 16;
                }
            }

            return Decimalise(builderN.ToString(), radii);
        }

        private static bool ExtractSubValue(string number, int offset, out int radii)
        {
            int radix = 0;
            radii = 10;
            var builderR = new StringBuilder();
            for (int c = offset; c < number.Length; c++)
            {
                if (radix == 1)
                    builderR.Append(number[c] - '₀');
                else if (radix == 2)
                {
                    if (number[c] != '_')
                        return false;
                    radix++;
                }
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
                        return false;
                }
            }
            return int.TryParse(builderR.ToString(), System.Globalization.NumberStyles.Integer, null, out radii);
        }

        protected bool IsLegalNumberFormat(string number)
        {
            return ConvertNumber(number) != null;
        }

        public IEnumerable<Result<Tokens>> Tokenize(string input, string path="")
        {
			return Tokenize(new TokenSpan(path, input));
        }

        public IEnumerable<Result<Tokens>> TokenizeFromFile(string filename)
        {
            return Tokenize(new TokenSpan(filename, System.IO.File.ReadAllText(filename)));
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
                    var location = next.Location;
                    var remainder = next.Remainder;
                    next = next.Remainder.ConsumeChar();
                    if (_dualOperators.TryGetValue((c, next.Value), out var dualToken))
                    {
                        var secondC = next.Value;
                        remainder = next.Remainder;
                        token = dualToken;
                        next = next.Remainder.ConsumeChar();
                        if (_tripleOperators.TryGetValue((c, secondC, next.Value), out var tripleToken))
                        {
                            remainder = next.Remainder;
                            token = tripleToken;
                            next = next.Remainder.ConsumeChar();
                        }
                    }
                    yield return new Result<Tokens>(token, location, remainder);
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
                            next = SkipToEndCommentBlock(next.Remainder);
                            yield return new Result<Tokens>(Tokens.MultiLineComment, start, next.Location);
                        }
                        else
                        {
                            next = SkipToNewLine(start);
                            yield return new Result<Tokens>(Tokens.SingleComment, start, next.Location);
                        }
                    }
                }
                else if (c == '"')
                {
                    var start = next;
                    next = next.Remainder.ConsumeChar();
                    next = SkipToEndString(next.Location);

                    if (VerifyStringLiteral(start.Location.Until(next.Location).ToStringValue()))
                    {
                        yield return new Result<Tokens>(Tokens.String, start.Location, next.Location);
                    }
                    else
                    {
                        messages.Log(CompilerErrorKind.Error_FailedToFindEndOfString, $"Missing '\"' terminator for string literal", start.Location);
                        yield break;
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
                    messages.Log(CompilerErrorKind.Error_InvalidToken, $"Unexpected character ('{c}' U+{(ushort)c:X4} {Char.GetUnicodeCategory(c).ToString()})", next.Location);
                    next = next.Remainder.ConsumeChar();
                }

                next = SkipWhiteSpace(next.Location);
            } while (next.HasValue);
        }
    }
}