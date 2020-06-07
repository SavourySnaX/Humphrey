using sly.lexer;
using sly.parser.generator;

namespace Humphrey.FrontEnd
{
    public class Parser
    {
        [Production("return_list : Bit")]
        public string ReturnList(Token<Tokens> bit)
        {
            return "ReturnList";
        }

        [Production("param_list : Bit Identifier")]
        public string ParamList(Token<Tokens> bit, Token<Tokens> identifier)
        {
            return "ParamList";
        }

        [Production("block : OpenCurlyBrace [d] statement CloseCurlyBrace [d]")]
        public string Block(string statement)
        {
            return "Block";
        }

        [Production("statement : Return [d] Identifier SemiColon [d]")]
        public string Statement(Token<Tokens> statement)
        {
            return "Statement";
        }

        [Production("function : return_list Identifier OpenParanthesis [d] param_list CloseParanthesis [d] block")]
        public string FunctionDecleration(string return_list, Token<Tokens> identifier, string param_list, string block)
        {
            return $"{return_list} {identifier.Value} {param_list} {block}";
        }
    }
}
