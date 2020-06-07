using sly.lexer;

namespace Humphrey.FrontEnd
{
    public enum Tokens
    {
        [Lexeme(GenericToken.Identifier, IdentifierType.Custom, "_a-zA-Z", "_a-zA-Z0-9")]
        Identifier,
        [Lexeme(GenericToken.KeyWord, "bit")]
        Bit,
        [Lexeme(GenericToken.KeyWord, "return")]
        Return,
        [Lexeme(GenericToken.SugarToken, "+")]
        Plus,
        [Lexeme(GenericToken.SugarToken, ";")]
        SemiColon,
        [Lexeme(GenericToken.SugarToken, "{")]
        OpenCurlyBrace,
        [Lexeme(GenericToken.SugarToken, "}")]
        CloseCurlyBrace,
        [Lexeme(GenericToken.SugarToken, "(")]
        OpenParanthesis,
        [Lexeme(GenericToken.SugarToken, ")")]
        CloseParanthesis,
        [Lexeme(GenericToken.SugarToken, ",")]
        Comma,
        [SingleLineComment("#")]
        SingleComment
    }

}
