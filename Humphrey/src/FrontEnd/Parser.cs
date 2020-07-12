using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        IEnumerator<Result<Tokens>> tokens;

        public HumphreyParser(IEnumerable<Result<Tokens>> toParse)
        {
            tokens = toParse.GetEnumerator();
            NextToken();
        }

        bool IsSkippableToken(Tokens token)
        {
            return (token == Tokens.SingleComment || token == Tokens.MultiLineComment);
        }

        void NextToken()
        {
            do
            {
                if (tokens.MoveNext())
                    lookahead = tokens.Current;
                else
                    lookahead = new Result<Tokens>();
            } while (lookahead.HasValue && IsSkippableToken(lookahead.Value));
        }

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
        protected IAst[] ItemList(AstItemDelegate kind)
        {
            var list = new List<IAst>();
            while (true)
            {
                var item = kind();
                if (item != null)
                    list.Add(item);
                else
                    break;
            }

            return list.ToArray();
        }

        public delegate (bool success, string item) ItemDelegate();
        public delegate IAst AstItemDelegate();
        public delegate IAst AstInitDelegate(string item);

        // | (1 of)
        protected IAst OneOf(AstItemDelegate[] kinds)
        {
            foreach (var k in kinds)
            {
                var t = k();
                if (t != null)
                    return t;
            }

            return null;
        }

        // 0 or more ( | )
        protected IAst[] ManyOf(AstItemDelegate[] kinds)
        {
            var list = new List<IAst>();
            while (true)
            {
                var t = OneOf(kinds);
                if (t != null)
                    list.Add(t);
                else
                    break;
            }

            return list.ToArray();
        }

        protected IAst AstItem(Tokens kind, AstInitDelegate init)
        {
            var item = Item(kind);
            if (item.success)
                return init(item.item);

            return null;
        }

        // number : Number
        public AstNumber Number() { return AstItem(Tokens.Number, (e) => new AstNumber(e)) as AstNumber; }

        // identifier : Identifier
        public AstIdentifier Identifier() { return AstItem(Tokens.Identifier, (e) => new AstIdentifier(e)) as AstIdentifier; }

        // number_list : Number*
        public IAst[] NumberList() { return ItemList(Number); }

        // identifer_list : Identifier*        
        public IAst[] IdentifierList() { return ItemList(Identifier); }

        // bit_keyword : bit
        public AstBitType BitKeyword() { return AstItem(Tokens.KW_Bit, (e) => new AstBitType()) as AstBitType; }
        public IAst ReturnKeyword() { return AstItem(Tokens.KW_Return, (e) => new AstKeyword(e)); }

        // add_operator : +
        public IAst AddOperator() { return AstItem(Tokens.O_Plus, (e) => new AstOperator(e)); }
        // subtract_operator : -
        public IAst SubOperator() { return AstItem(Tokens.O_Subtract, (e) => new AstOperator(e)); }
        // multiply_operator : *
        public IAst MultiplyOperator() { return AstItem(Tokens.O_Multiply, (e) => new AstOperator(e)); }
        // divide_operator : /
        public IAst DivideOperator() { return AstItem(Tokens.O_Divide, (e) => new AstOperator(e)); }
        // modulus_operator : %
        public IAst ModulusOperator() { return AstItem(Tokens.O_Modulus, (e) => new AstOperator(e)); }
        // equals_operator : Equals
        public IAst EqualsOperator() { return AstItem(Tokens.O_Equals, (e) => new AstOperator(e)); }
        public IAst ColonOperator() { return AstItem(Tokens.O_Colon, (e) => new AstOperator(e)); }
        public bool CommaSyntax() { return Item(Tokens.S_Comma).success; }
        public bool SemiColonSyntax() { return Item(Tokens.S_SemiColon).success; }
        public bool OpenParanthesis() { return Item(Tokens.S_OpenParanthesis).success; }
        public bool CloseParenthesis() { return Item(Tokens.S_CloseParanthesis).success; }
        public bool OpenCurlyBrace() { return Item(Tokens.S_OpenCurlyBrace).success; }
        public bool CloseCurlyBrace() { return Item(Tokens.S_CloseCurlyBrace).success; }
        public bool OpenSquareBracket() { return Item(Tokens.S_OpenSquareBracket).success; }
        public bool CloseSquareBracket() { return Item(Tokens.S_CloseSquareBracket).success; }
        public bool UnderscoreOperator() { return Item(Tokens.S_Underscore).success; }

        public AstItemDelegate[] UnaryOperators => new AstItemDelegate[] { AddOperator, SubOperator };
        public AstItemDelegate[] BinaryOperators => new AstItemDelegate[] { AddOperator, SubOperator, MultiplyOperator, DivideOperator, ModulusOperator };
        public AstItemDelegate[] ExpressionKind => new AstItemDelegate[] { UnderscoreExpression, UnaryExpression, BinaryExpression };
        public AstItemDelegate[] Types => new AstItemDelegate[] { ArrayType, BitKeyword, Identifier, FunctionType, StructType };
        public AstItemDelegate[] NonFunctionTypes => new AstItemDelegate[] { ArrayType, BitKeyword, Identifier, StructType };
        public AstItemDelegate[] Assignables => new AstItemDelegate[] {  CodeBlock, ParseExpression };
        public AstItemDelegate[] Statements => new AstItemDelegate[] { CodeBlock, ReturnStatement };

        public AstItemDelegate[] StructDefinitions => new AstItemDelegate[] { StructElement };
        public AstItemDelegate[] GlobalDefinition => new AstItemDelegate[] { Definition };

        // terminal : Number | Identifier | BracketedExpression
        public AstItemDelegate[] Terminal => new AstItemDelegate[] { Number, Identifier, BracketedExpression };

        // bracketed_expresson : ( Expression )
        public IAst BracketedExpression()
        {
            if (!OpenParanthesis())
                return null;
            PushSentinel();
            var expr = Expression();
            if (expr == null)
                return null;
            if (!CloseParenthesis())
                return null;
            return PopSentinel();
        }

        Stack<(bool binary, IOperator item)> operators;
        Stack<IExpression> operands;

        public IExpression ParseExpression()
        {
            operators = new Stack<(bool binary, IOperator item)>();
            operands = new Stack<IExpression>();
            PushSentinel();
            var expr = Expression();
            if (expr == null)
                return null;
            return PopSentinel();
        }

        bool IsTopLowerPrecedance(IOperator op)
        {
            var peek = operators.Peek();
            int top = peek.item == null ? int.MaxValue : peek.item.Precedance;
            int currentOp = op.Precedance;
            return currentOp >= top;
        }

        public void PopOperator()
        {
            if (operators.Peek().binary)
            {
                var i2 = operands.Pop();
                var i1 = operands.Pop();
                operands.Push(AstBinaryExpression.FetchBinaryExpression(operators.Pop().item, i1, i2));
            }
            else
            {
                operands.Push(AstUnaryExpression.FetchUnaryExpression(operators.Pop().item, operands.Pop()));
            }
        }

        public void PushSentinel()
        {
            operators.Push((false, null));
        }

        public IExpression PopSentinel()
        {
            if (operators.Pop().item != null)
                return null;
            return operands.Pop();
        }

        public void PushOperator((bool binary, IOperator item) op)
        {
            while (IsTopLowerPrecedance(op.item))
            {
                PopOperator();
            }
            operators.Push(op);
        }

        // expression : UnaryExpression
        //            | BinaryExpression
        public IExpression Expression()
        {
            var expr = OneOf(ExpressionKind) as IExpression;
            while (operators.Peek().item != null)
                PopOperator();
            return expr;
        }

        // binary_expression : Terminal
        //                   | Terminal operator expression
        public IExpression BinaryExpression()
        {
            var terminal = OneOf(Terminal) as IExpression;
            if (terminal != null)
            {
                operands.Push(terminal);
                var op = OneOf(BinaryOperators) as IOperator;
                if (op != null)
                {
                    PushOperator((true, op));
                    var expr = Expression();
                    if (expr != null)
                        return AstBinaryExpression.FetchBinaryExpression(op, terminal, expr);

                    return null;
                }

                return terminal;
            }

            return null;
        }

        // underscore_expression : underscore_operator 
        public IExpression UnderscoreExpression()
        {
            if (!UnderscoreOperator())
                return null;

            var operand = new AstUnderscoreExpression();
            operands.Push(operand);
            return operand;
        }

        // unary_expression : unary_operator expression
        public IExpression UnaryExpression()
        {
            var op = OneOf(UnaryOperators) as IOperator;
            if (op == null)
                return null;
            PushOperator((false, op));
            var expr = Expression();
            if (expr != null)
                return AstUnaryExpression.FetchUnaryExpression(op, expr);

            return null;
        }

        // Root
        public IAst[] File() { return ManyOf(GlobalDefinition); }

        // param_definition : identifier : type
        public AstParamDefinition ParamDefinition()
        {
            var identifier = Identifier();
            if (identifier == null)
                return null;

            if (ColonOperator() == null)
                return null;

            var typeSpecifier = Type();
            if (typeSpecifier == null)
                return null;

            return new AstParamDefinition(identifier, typeSpecifier);
        }

        // param_definition_list : param_definition
        //                       | param_definition , param_defitinition_list
        public AstParamDefinition[] ParamDefinitionList()
        {
            var list = new List<AstParamDefinition>();

            var param = ParamDefinition();
            if (param == null)
                return null;

            list.Add(param);

            while (CommaSyntax())
            {
                param = ParamDefinition();
                if (param == null)
                    return null;

                list.Add(param);
            }

            return list.ToArray();
        }

        // expression_list : expr
        //                 | expr , expression_list
        public IExpression[] ExpressionList()
        {
            var list = new List<IExpression>();

            var expr = ParseExpression();
            if (expr == null)
                return null;

            list.Add(expr);

            while (CommaSyntax())
            {
                expr = ParseExpression();
                if (expr == null)
                    return null;

                list.Add(expr);
            }

            return list.ToArray();
        }

        // parameter_list : ( param_definition_list )
        //                | ( )
        public AstParamList ParamList()
        {
            if (!OpenParanthesis())
                return null;

            if (CloseParenthesis())
                return new AstParamList(new AstParamDefinition[] { });

            var paramDefinitionList = ParamDefinitionList();
            if (paramDefinitionList == null)
                return null;

            if (!CloseParenthesis())
                return null;

            return new AstParamList(paramDefinitionList);
        }

        // array_type : [ConstantExpr] bit|identifier|functionType|structType
        public AstArrayType ArrayType()
        {
            if (!OpenSquareBracket())
                return null;

            var expr = ParseExpression();
            if (expr==null)
                return null;

            if (!CloseSquareBracket())
                return null;

            var typeSpecifier = OneOf(NonFunctionTypes);
            if (typeSpecifier == null)
                return null;

            return new AstArrayType(expr, typeSpecifier as IType);
        }

        // function_type : parameter_list parameter_list
        public AstFunctionType FunctionType()
        {
            var inputs = ParamList();
            if (inputs == null)
                return null;

            var outputs = ParamList();
            if (outputs == null)
                return null;

            return new AstFunctionType(inputs, outputs);
        }
        
        // struct_type : { struct_element* }
        public AstStructureType StructType()
        {
            if (!OpenCurlyBrace())
                return null;

            var definitionList = new List<AstStructElement>();
            AstStructElement def = null;
            do
            {
                def = OneOf(StructDefinitions) as AstStructElement;
                if (def != null)
                    definitionList.Add(def);
            } while (def != null);

            if (!CloseCurlyBrace())
                return null;

            return new AstStructureType(definitionList.ToArray());
        }

        // type : bit                       // builtin
        //      | identifier                // type
        //      | struct_type               // struct
        //      | function_type             // function
        public IType Type() { return OneOf(Types) as IType; }
        public IType NonFunctionType() { return OneOf(NonFunctionTypes) as IType; }

        // assignable : { statements }      // function body
        //            | expression
        public IAssignable Assignable() { return OneOf(Assignables) as IAssignable; }

        // struct_element : identifier : non_function_type
        //                | identifier = assignable
        //                | identifier : non_function_type = assignable
        public AstStructElement StructElement()
        {
            var identifier = Identifier();
            if (identifier == null)
                return null;

            bool hadType = false;
            bool hadValue = false;

            IType typeSpecifier = null;
            IAssignable assignable = null;

            if (ColonOperator() != null)
            {
                hadType = true;
                typeSpecifier = NonFunctionType();
                if (typeSpecifier == null)
                    return null;
            }

            if (EqualsOperator() != null)
            {
                hadValue = true;
                assignable = Assignable();
                if (assignable == null)
                    return null;
            }

            if (!hadType && !hadValue)
                return null;

            return new AstStructElement(identifier, typeSpecifier, assignable);
        }

        // definition : identifier : type
        //            | identifier = assignable
        //            | identifier : type = assignable
        public AstDefinition Definition()
        {
            var identifier = Identifier();
            if (identifier == null)
                return null;

            bool hadType = false;
            bool hadValue = false;

            IType typeSpecifier = null;
            IAssignable assignable = null;

            if (ColonOperator() != null)
            {
                hadType = true;
                typeSpecifier = Type();
                if (typeSpecifier == null)
                    return null;
            }

            if (EqualsOperator() != null)
            {
                hadValue = true;
                assignable = Assignable();
                if (assignable == null)
                    return null;
            }

            if (!hadType && !hadValue)
                return null;

            return new AstDefinition(identifier, typeSpecifier, assignable);
        }

        // return_statement : return [expression_list] ;
        public AstReturnStatement ReturnStatement()
        {
            if (ReturnKeyword() == null)
                return null;

            if (SemiColonSyntax())
                return new AstReturnStatement(new IExpression[] { });

            var expressionList = ExpressionList();
            if (expressionList==null)
                return null;

            if (!SemiColonSyntax())
                return null;

            return new AstReturnStatement(expressionList);
        }

        // statement : block
        //           | keyword..
        //           | assignment
        public IStatement Statement() { return OneOf(Statements) as IStatement; }

        // statement_list : statement
        //                | statement statement_list
        public IStatement[] StatementList()
        {
            var list = new List<IStatement>();

            while (true)
            {
                var statement = Statement();
                if (statement == null)
                    break;
                list.Add(statement);
            } 

            return list.ToArray();
        }

        // code_block : { statement_list* }
        public AstCodeBlock CodeBlock()
        {
            if (!OpenCurlyBrace())
                return null;

            var statements = StatementList();
            if (statements == null)
                return null;

            if (!CloseCurlyBrace())
                return null;

            return new AstCodeBlock(statements);
        }

    }
}
