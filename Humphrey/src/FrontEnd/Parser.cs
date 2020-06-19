using System.Collections.Generic;
using System.Linq;

namespace Humphrey.FrontEnd
{
    [System.Serializable]
    public class ParseException : System.Exception
    {
        public ParseException() { }
        public ParseException(string message) : base(message) { }
        public ParseException(string message, System.Exception inner) : base(message, inner) { }
        protected ParseException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public class HumphreyParser
    {
        public HumphreyParser(IEnumerable<Result<Tokens>> toParse)
        {
            tokens = new Queue<Result<Tokens>>(toParse);
            NextToken();
        }

        void NextToken()
        {
            if (tokens.Count != 0)
                lookahead = tokens.Dequeue();
            else
                lookahead = new Result<Tokens>();
        }

        Queue<Result<Tokens>> tokens;
        Result<Tokens> lookahead;

        (bool success, string item) Item(Tokens kind)
        {
            if (lookahead.HasValue && lookahead.Value == kind)
            {
                var v = lookahead.ToStringValue();
                if (kind == Tokens.Number)
                    v = HumphreyTokeniser.ConvertNumber(v);
                NextToken();
                return (true, v);
            }
            return (false, "");
        }

        // * (0 or more)
        protected (bool success, string[] items) ItemList(Tokens kind)
        {
            var list = new List<string>();
            while (true)
            {
                var (success, item) = Item(kind);
                if (success)
                    list.Add(item);
                else
                    break;
            }

            return (true, list.ToArray());
        }

        public delegate (bool success, string item) ItemDelegate();

        // | (1 of)
        protected (bool success, string item) OneOf(ItemDelegate[] kinds)
        {
            foreach (var k in kinds)
            {
                var t = k();
                if (t.success)
                    return t;
            }

            return (false, "");
        }

        // number : Number
        public (bool success, string) Number() { return Item(Tokens.Number); }

        // identifier : Identifier
        public (bool success, string) Identifier() { return Item(Tokens.Identifier); }

        // number_list : Number*
        public (bool success, string[]) NumberList() { return ItemList(Tokens.Number); }

        // identifer_list : Identifier*        
        public (bool success, string[]) IdentifierList() { return ItemList(Tokens.Identifier); }

        // add_operator : Plus
        public (bool success, string) AddOperator() { return Item(Tokens.O_Plus); }
        // add_operator : Plus
        public (bool success, string) SubOperator() { return Item(Tokens.O_Subtract); }

        public ItemDelegate[] UnaryOperators => new ItemDelegate[] { AddOperator, SubOperator };
        public ItemDelegate[] BinaryOperators => new ItemDelegate[] { AddOperator, SubOperator };

        // terminal : Number | Identifier | BracketedExpression
        public ItemDelegate[] Terminal => new ItemDelegate[] { Number, Identifier, BracketedExpression };

        // bracketed_expresson : ( Expression )
        public (bool success, string item) BracketedExpression()
        {
            if (!Item(Tokens.S_OpenParanthesis).success)
                return (false, "");
            var expr = BinaryExpression();
            if (!expr.success)
                return (false, "");
            if (!Item(Tokens.S_CloseParanthesis).success)
                return (false, "");
            return expr;
        }

        // binary_expression : Terminal
        //                   | Terminal operator expression
        public (bool success, string item) BinaryExpression()
        {
            var terminal = OneOf(Terminal);
            if (terminal.success)
            {
                var op = OneOf(BinaryOperators);
                if (op.success)
                {
                    var binExpr = BinaryExpression();
                    if (binExpr.success)
                        return (true, $"{op.item} {terminal.item} {binExpr.item}");

                    return (false, "");
                }

                return (true, terminal.item);
            }

            return (false, "");
        }

        // unary_expression : unary_operator expression
        public (bool success, string item) UnaryExpression()
        {
            var op = OneOf(UnaryOperators);
            if (!op.success)
                return (false, "");
            var binExpr = BinaryExpression();
            if (binExpr.success)
                return (true, $"{op.item} {binExpr.item}");

            return (false, "");
        }

        // Root
        public (bool success, string[]) File() { return IdentifierList(); }
    }
}
