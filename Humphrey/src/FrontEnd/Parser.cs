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

        // 0 or more ( | )
        protected (bool success, string[] items) ManyOf(ItemDelegate[] kinds)
        {
            var list = new List<string>();
            while (true)
            {
                var (success, item) = OneOf(kinds);
                if (success)
                    list.Add(item);
                else
                    break;
            }

            return (true, list.ToArray());
        }

        // number : Number
        public (bool success, string item) Number() { return Item(Tokens.Number); }

        // identifier : Identifier
        public (bool success, string item) Identifier() { return Item(Tokens.Identifier); }

        // number_list : Number*
        public (bool success, string[] items) NumberList() { return ItemList(Tokens.Number); }

        // identifer_list : Identifier*        
        public (bool success, string[] items) IdentifierList() { return ItemList(Tokens.Identifier); }

        // bit_keyword : bit
        public (bool success, string item) BitKeyword() { return Item(Tokens.KW_Bit); }
        // add_operator : Plus
        public (bool success, string item) AddOperator() { return Item(Tokens.O_Plus); }
        // subtract_operator : Sub
        public (bool success, string item) SubOperator() { return Item(Tokens.O_Subtract); }
        // multiply_operator : Plus
        public (bool success, string item) MultiplyOperator() { return Item(Tokens.O_Multiply); }
        // divide_operator : Sub
        public (bool success, string item) DivideOperator() { return Item(Tokens.O_Divide); }

        // equals_operator : Equals
        public (bool success, string item) EqualsOperator() { return Item(Tokens.O_Equals); }
        // colon_operator : Equals
        public (bool success, string item) ColonOperator() { return Item(Tokens.O_Colon); }

        public ItemDelegate[] UnaryOperators => new ItemDelegate[] { AddOperator, SubOperator };
        public ItemDelegate[] BinaryOperators => new ItemDelegate[] { AddOperator, SubOperator, MultiplyOperator, DivideOperator };
        public ItemDelegate[] ExpressionKind => new ItemDelegate[] { UnaryExpression, BinaryExpression };
        public ItemDelegate[] Types => new ItemDelegate[] { BitKeyword, Identifier/*, FunctionType, StructType*/ };
        public ItemDelegate[] Assignables => new ItemDelegate[] { /* block, */ ParseExpression };

        public ItemDelegate[] GlobalDefinition => new ItemDelegate[] { Definition };

        // terminal : Number | Identifier | BracketedExpression
        public ItemDelegate[] Terminal => new ItemDelegate[] { Number, Identifier, BracketedExpression };

        // bracketed_expresson : ( Expression )
        public (bool success, string item) BracketedExpression()
        {
            if (!Item(Tokens.S_OpenParanthesis).success)
                return (false, "");
            operators.Push((false, ""));
            var expr = Expression();
            if (!expr.success)
                return (false, "");
            if (!Item(Tokens.S_CloseParanthesis).success)
                return (false, "");
            return PopSentinel();
        }

        Stack<(bool binary, string item)> operators;
        Stack<string> operands;

        public (bool success, string item) ParseExpression()
        {
            operators = new Stack<(bool binary, string item)>();
            operands = new Stack<string>();
            operators.Push((false, ""));
            var (result, _) = Expression();
            operators.Pop();
            return (result, result ? operands.Pop() : "");
        }

        int Precedance(string op)
        {
            switch (op)
            {
                case "":
                    return int.MaxValue;
                case "+":
                    return 500;
                case "-":
                    return 500;
                case "*":
                    return 300;
                case "/":
                    return 300;
                case "=":
                    return 150;
                case ":":
                    return 100;
                default:
                    throw new ParseException($"Unimplemented Precadnce for operator : {op}");
            }
        }

        bool IsTopLowerPrecedance(string op)
        {
            int top = Precedance(operators.Peek().item);
            int currentOp = Precedance(op);
            return currentOp >= top;
        }

        public void PopOperator()
        {
            if (operators.Peek().binary)
            {
                var i2 = operands.Pop();
                var i1 = operands.Pop();
                operands.Push($"{operators.Pop().item} {i1} {i2}");
            }
            else
            {
                operands.Push($"{operators.Pop().item} {operands.Pop()}");
            }
        }

        public (bool success, string item) PopSentinel()
        {
            if (operators.Pop().item != "")
                return (false, "");
            return (true, operands.Pop());
        }

        public void PushOperator((bool binary, string item) op)
        {
            while (IsTopLowerPrecedance(op.item))
            {
                PopOperator();
            }
            operators.Push(op);
        }

        // expression : UnaryExpression
        //            | BinaryExpression
        public (bool success, string item) Expression()
        {
            var (result, _) = OneOf(ExpressionKind);
            while (operators.Peek().item != "")
                PopOperator();
            return (result, "");
        }

        // binary_expression : Terminal
        //                   | Terminal operator expression
        public (bool success, string item) BinaryExpression()
        {
            var terminal = OneOf(Terminal);
            if (terminal.success)
            {
                operands.Push(terminal.item);
                var op = OneOf(BinaryOperators);
                if (op.success)
                {
                    PushOperator((true, op.item));
                    var expr = Expression();
                    if (expr.success)
                        return (true, $"{op.item} {terminal.item} {expr.item}");

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
            PushOperator((false, op.item));
            var expr = Expression();
            if (expr.success)
                return (true, $"{op.item} {expr.item}");

            return (false, "");
        }

        // Root
        public (bool success, string[]) File() { return ManyOf(GlobalDefinition); }

        // param_definition : identifier : type

        // param_definition_list : param_definition
        //                       | param_definition , param_defitinition_list

        // parameter_list : ( param_definition_list )
        //                | ( )

        // function_type : parameter_list parameter_list

        // type : bit                       // builtin
        //      | identifier                // type
        //      | struct_type               // struct
        //      | function_type             // function
        public (bool success, string item) Type() { return OneOf(Types); }

        // assignable : { statements }      // function body
        //            | expression
        public (bool success, string item) Assignable() { return OneOf(Assignables); }

        // definition : identifier : type
        //            | identifier = assignable
        //            | identifier : type = assignable
        public (bool success, string item) Definition()
        {
            string returnValue = "";
            var identifier = Identifier();
            if (!identifier.success)
                return (false, "");

            returnValue += identifier.item;
            bool hadType = false;
            bool hadValue = false;
            if (Item(Tokens.O_Colon).success)
            {
                hadType = true;
                var typeSpecifier = Type();
                if (!typeSpecifier.success)
                    return (false, "");
                returnValue += $" : {typeSpecifier.item}";
            }
            
            if (Item(Tokens.O_Equals).success)
            {
                hadValue = true;
                var assignable = Assignable();
                if (!assignable.success)
                    return (false, "");
                returnValue += $" = {assignable.item}";
            }

            if (!hadType&&!hadValue)
                return (false, "");

            return (true, returnValue);

        }

    }
}
