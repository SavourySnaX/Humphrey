using System.Collections.Generic;

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
            operators = new Stack<(bool binary, IOperator item)>();
            operands = new Stack<IAst>();
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
        (bool success, string item) Peek(Tokens kind)
        {
            if (lookahead.HasValue && lookahead.Value == kind)
            {
                var v = lookahead.ToStringValue();
                if (kind == Tokens.Number)
                    v = HumphreyTokeniser.ConvertNumber(v);
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
        
        // + (1 or more)
        protected T[] CommaSeperatedItemList<T>(AstItemDelegate kind) where T : class
        {
            var list = new List<T>();

            T item = kind() as T;
            if (item == null)
                return null;
            list.Add(item);

            while (CommaSyntax())
            {
                item = kind() as T;
                if (item == null)
                    return null;
                list.Add(item);
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
        protected T[] ManyOf<T>(AstItemDelegate[] kinds) where T : class
        {
            var list = new List<T>();
            while (true)
            {
                var t = OneOf(kinds) as T;
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
        // loadable_identifier : Identifier
        public AstLoadableIdentifier LoadableIdentifier() { return AstItem(Tokens.Identifier, (e) => new AstLoadableIdentifier(e)) as AstLoadableIdentifier; }

        // number_list : Number*
        public IAst[] NumberList() { return ItemList(Number); }

        // identifer_list : Identifier*        
        public AstIdentifier[] IdentifierList() { return CommaSeperatedItemList<AstIdentifier>(Identifier); }

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
        // as_operator : %
        public IAst AsOperator() { return AstItem(Tokens.O_As, (e) => new AstOperator(e)); }
        // reference_operator : .
        public IAst ReferenceOperator() { return AstItem(Tokens.O_Dot, (e) => new AstOperator(e)); }
        // function_call_operator : (
        public IAst FunctionCallOperator() { return AstItem(Tokens.S_OpenParanthesis, (e) => new AstOperator(e)); }
        // function_call_operator : [
        public IAst ArraySubscriptOperator() { return AstItem(Tokens.S_OpenSquareBracket, (e) => new AstOperator(e)); }
        // equals_operator : Equals
        public IAst EqualsOperator() { return AstItem(Tokens.O_Equals, (e) => new AstOperator(e)); }
        public IAst ColonOperator() { return AstItem(Tokens.O_Colon, (e) => new AstOperator(e)); }
        public bool CommaSyntax() { return Item(Tokens.S_Comma).success; }
        public bool SemiColonSyntax() { return Item(Tokens.S_SemiColon).success; }
        public bool OpenParanthesis() { return Item(Tokens.S_OpenParanthesis).success; }
        public bool PeekOpenParanthesis() { return Peek(Tokens.S_OpenParanthesis).success; }
        public bool CloseParenthesis() { return Item(Tokens.S_CloseParanthesis).success; }
        public bool OpenCurlyBrace() { return Item(Tokens.S_OpenCurlyBrace).success; }
        public bool CloseCurlyBrace() { return Item(Tokens.S_CloseCurlyBrace).success; }
        public bool PeekCloseCurlyBrace() { return Peek(Tokens.S_CloseCurlyBrace).success; }
        public bool OpenSquareBracket() { return Item(Tokens.S_OpenSquareBracket).success; }
        public bool PeekOpenSquareBracket() { return Peek(Tokens.S_OpenSquareBracket).success; }
        public bool CloseSquareBracket() { return Item(Tokens.S_CloseSquareBracket).success; }
        public bool UnderscoreOperator() { return Item(Tokens.S_Underscore).success; }
        public bool PointerOperator() { return Item(Tokens.O_Multiply).success; }

        public AstItemDelegate[] UnaryOperators => new AstItemDelegate[] { AddOperator, SubOperator, MultiplyOperator };
        public AstItemDelegate[] BinaryOperators => new AstItemDelegate[] { AddOperator, SubOperator, MultiplyOperator, DivideOperator, ModulusOperator, AsOperator, ReferenceOperator, FunctionCallOperator, ArraySubscriptOperator };
        public AstItemDelegate[] ExpressionKind => new AstItemDelegate[] { UnderscoreExpression, UnaryExpression, BinaryExpression };
        public AstItemDelegate[] Types => new AstItemDelegate[] { PointerType, ArrayType, BitKeyword, Identifier, FunctionType, StructType };
        public AstItemDelegate[] NonFunctionTypes => new AstItemDelegate[] { PointerType, ArrayType, BitKeyword, Identifier, StructType };
        public AstItemDelegate[] Assignables => new AstItemDelegate[] {  CodeBlock, ParseExpression };
        public AstItemDelegate[] Statements => new AstItemDelegate[] { CodeBlock, ReturnStatement, LocalScopeDefinition };

        public AstItemDelegate[] StructDefinitions => new AstItemDelegate[] { StructElementDefinition };
        public AstItemDelegate[] LocalDefinition => new AstItemDelegate[] { LocalScopeDefinition };
        public AstItemDelegate[] GlobalDefinition => new AstItemDelegate[] { GlobalScopeDefinition };

        // terminal : Number | IdentifierTerminal | BracketedExpression
        public AstItemDelegate[] Terminal => new AstItemDelegate[] { Number, IdentifierTerminal, BracketedExpression };

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


        // identifier_terminal : identifier                         # variable
        public ILoadValue IdentifierTerminal()
        {
            return LoadableIdentifier();
        }

        Stack<(bool binary, IOperator item)> operators;
        Stack<IAst> operands;

        // expression = expression
        //            | 
        public IExpression ParseExpression()
        {
            PushSentinel();
            var expr = Expression();
            if (expr == null)
                return null;
            //
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
                switch (operators.Peek().item.RhsKind)
                {
                    case IOperator.OperatorKind.ExpressionExpression:
                        operands.Push(AstBinaryExpression.FetchBinaryExpression(operators.Pop().item, i1 as IExpression, i2 as IExpression));
                        break;
                    case IOperator.OperatorKind.ExpressionType:
                        operands.Push(AstBinaryExpression.FetchBinaryExpressionRhsType(operators.Pop().item, i1 as IExpression, i2 as IType));
                        break;
                    case IOperator.OperatorKind.ExpressionIdentifier:
                        operands.Push(AstBinaryExpression.FetchBinaryExpressionRhsIdentifer(operators.Pop().item, i1 as IExpression, i2 as AstIdentifier));
                        break;
                    case IOperator.OperatorKind.ExpressionExpressionList:
                        operands.Push(AstBinaryExpression.FetchBinaryExpressionRhsExpressionList(operators.Pop().item, i1 as IExpression, i2 as AstExpressionList));
                        break;
                    case IOperator.OperatorKind.ExpressionExpressionContinuation:
                        operands.Push(AstBinaryExpression.FetchBinaryExpressionRhsExpressionContinuation(operators.Pop().item, i1 as IExpression, i2 as IExpression));
                        break;
                    default:
                        throw new System.NotImplementedException($"Unhandled RhsKind");
                }
            }
            else
            {
                operands.Push(AstUnaryExpression.FetchUnaryExpression(operators.Pop().item, operands.Pop() as IExpression));
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
            return operands.Pop() as IExpression;
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
            if (expr == null)
                return null;
            while (operators.Peek().item != null)
                PopOperator();
            return expr;
        }

        // expression_type : Type
        public IType ExpressionType()
        {
            var type = Type();
            operands.Push(type);
            while (operators.Peek().item != null)
                PopOperator();
            return type;
        }

        // [ has already popped at this point
        // array_subscript : expression ]
        public IExpression ArraySubscript()
        {
            var expr = ParseExpression();
            if (expr == null)
                return null;
            if (!CloseSquareBracket())
                return null;
            return expr;
        }

        // ( has already popped at this point
        // function_call_arguments : )
        //                         | expression_list )
        public AstExpressionList FunctionCallArguments()
        {
            AstExpressionList exprList;

            if (CloseParenthesis())
            {
                exprList = new AstExpressionList();
            }
            else
            {
                exprList = ExpressionList();
                if (exprList == null)
                    return null;
                if (!CloseParenthesis())
                    return null;
            }
            return exprList;
        }

        // expression_function_call_arguments :  function_call_arguments
        public IExpression ExpressionFunctionCallArguments()
        {
            AstExpressionList exprList = FunctionCallArguments();
            if (exprList == null)
                return null;
            operands.Push(exprList);
            while (operators.Peek().item != null)
                PopOperator();
            var op = OneOf(BinaryOperators) as IOperator;
            var functionCall = operands.Peek() as IExpression;
            if (op!=null)
            {
                return BinaryOperatorProcess(functionCall, op);
            }
            return functionCall;
        }

        // expression_array_subscript : array_subscript
        public IExpression ExpressionArraySubscript()
        {
            IExpression subscript = ArraySubscript();
            if (subscript == null)
                return null;
            operands.Push(subscript);
            while (operators.Peek().item != null)
                PopOperator();
            var op = OneOf(BinaryOperators) as IOperator;
            var arrayidx = operands.Peek() as IExpression;
            if (op!=null)
            {
                return BinaryOperatorProcess(arrayidx, op);
            }
            return arrayidx;
        }


        // expression_identifier : identifier_terminal
        public IExpression ExpressionIdentifier()
        {
            var ident = Identifier();
            if (ident == null)
                return null;
            operands.Push(ident);
            while (operators.Peek().item != null)
                PopOperator();
            var op = OneOf(BinaryOperators) as IOperator;
            var terminal = operands.Peek() as IExpression;
            if (op!=null)
            {
                return BinaryOperatorProcess(terminal, op);
            }
            return terminal;
        }

        public IExpression BinaryOperatorProcess(IExpression terminal, IOperator op)
        {
            PushOperator((true, op));
            switch (op.RhsKind)
            {
                case IOperator.OperatorKind.ExpressionExpression:
                    var expr = Expression();
                    if (expr != null)
                        return AstBinaryExpression.FetchBinaryExpression(op, terminal, expr);
                    break;
                case IOperator.OperatorKind.ExpressionType:
                    var type = ExpressionType();
                    if (type != null)
                        return AstBinaryExpression.FetchBinaryExpressionRhsType(op, terminal, type);
                    break;
                case IOperator.OperatorKind.ExpressionIdentifier:
                    return ExpressionIdentifier();
                case IOperator.OperatorKind.ExpressionExpressionList:
                    return ExpressionFunctionCallArguments();
                case IOperator.OperatorKind.ExpressionExpressionContinuation:
                    return ExpressionArraySubscript();
            }
            return null;
        }

        // binary_expression : Terminal
        //                   | Terminal operator(+-/*%) expression
        //                   | Terminal operator(as) expression_type
        //                   | Terminal operator(.) expression_identifier
        public IExpression BinaryExpression()
        {
            var terminal = OneOf(Terminal) as IExpression;
            if (terminal != null)
            {
                operands.Push(terminal);
                var op = OneOf(BinaryOperators) as IOperator;
                if (op != null)
                {
                    return BinaryOperatorProcess(terminal, op);
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
        public IGlobalDefinition[] File() { return ManyOf<IGlobalDefinition>(GlobalDefinition); }

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
            return CommaSeperatedItemList<AstParamDefinition>(ParamDefinition);
        }

        // expression_list : expr
        //                 | expr , expression_list
        public AstExpressionList ExpressionList()
        {
            var exprList = CommaSeperatedItemList<IExpression>(ParseExpression);
            if (exprList == null)
                return null;
            return new AstExpressionList(exprList);
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

        // pointer_type : * bit|identifier|functionType|structType
        public AstPointerType PointerType()
        {
            if (!PointerOperator())
                return null;

            var typeSpecifier = OneOf(NonFunctionTypes);
            if (typeSpecifier == null)
                return null;

            return new AstPointerType(typeSpecifier as IType);
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

        // struct_element_definition : identifier : non_function_type
        //                           | identifier := assignable
        //                           | identifier : non_function_type = assignable
        public AstStructElement StructElementDefinition()
        {
            var (ok, identifierList, typeSpecifier, assignable) = Definition(Identifier, NonFunctionType, Assignable);
            if (!ok)
                return null;
            return new AstStructElement(identifierList, typeSpecifier, assignable);
        }

        // global_definition : identifier : non_function_type
        //                   | identifier := assignable
        //                   | identifier : non_function_type = assignable
        public AstGlobalDefinition GlobalScopeDefinition()
        {
            var (ok, identifierList, typeSpecifier, assignable) = Definition(Identifier, Type, Assignable);
            if (!ok)
                return null;
            return new AstGlobalDefinition(identifierList, typeSpecifier, assignable);
        }

        // local_definition  : identifier : non_function_type
        //                   | identifier := assignable
        //                   | identifier : non_function_type = assignable
        public AstLocalDefinition LocalScopeDefinition()
        {
            var (ok, identifierList, typeSpecifier, assignable) = Definition(Identifier, Type, Assignable);
            if (!ok)
                return null;
            return new AstLocalDefinition(identifierList, typeSpecifier, assignable);
        }

        // definition : identifier : type
        //            | identifier := assignable
        //            | identifier : type = assignable
        public (bool ok, AstIdentifier[] identifierList,IType typeSpecifier, IAssignable assignable) Definition(AstItemDelegate identifierDelegate,AstItemDelegate typeDelegate, AstItemDelegate assignableDelegate)
        {
            var identifier = CommaSeperatedItemList<AstIdentifier>(identifierDelegate);
            if (identifier == null)
                return (false, null, null, null);

            IType typeSpecifier = null;
            IAssignable assignable = null;

            if (ColonOperator() == null)
                return (false, null, null, null);

            typeSpecifier = typeDelegate() as IType;

            if (assignableDelegate != null)
            {
                if (EqualsOperator() != null)
                {
                    assignable = assignableDelegate() as IAssignable;
                    if (assignable == null)
                        return (false, null, null, null);
                }
            }

            if (typeSpecifier==null && assignable==null)
                return (false, null, null, null);

            return (true, identifier, typeSpecifier, assignable);
        }

        // return_statement : return [expression_list] ;
        public AstReturnStatement ReturnStatement()
        {
            if (ReturnKeyword() == null)
                return null;

            if (PeekCloseCurlyBrace())      // Must be last statement in a code block
                return new AstReturnStatement(new AstExpressionList());

            var expressionList = ExpressionList();
            if (expressionList==null)
                return null;

            if (!PeekCloseCurlyBrace())     // Must be last statement in a code block
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
