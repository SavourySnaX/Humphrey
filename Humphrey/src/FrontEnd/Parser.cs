using Superpower;
using Superpower.Model;
using Superpower.Parsers;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Humphrey.FrontEnd
{
    public class HumphreyParser
    {
        //static readonly TokenListParser<Tokens, string>


        /*
         *  # Test File
         * 
         *  main : 
         *  {
         *    add(1,1)
         *  }
         * 
         *  add : bit a,bit b
         *  {
         *    return a+b
         *  } bit result
         *
         */

        public static readonly TokenListParser<Tokens, string> Identifier =
            Token.EqualTo(Tokens.Identifier).Select(n => n.ToStringValue());

        public static readonly TokenListParser<Tokens, string[]> IdentifierList = Identifier.Many();

        public static readonly TokenListParser<Tokens, string[]> File = Identifier.Many();
    }



    /*
    public class Parser
    {
        static readonly TokenListParser<>


        [Production("file : function?")]
        public string File(string rule)
        {
            return "RootFile";
        }

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
    
    }*/
}
