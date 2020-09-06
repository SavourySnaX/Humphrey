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
        Result<Tokens> lookahead;

        Queue<Result<Tokens>> searchResetQueue;
        Queue<Result<Tokens>> searchResetBuffer;

        CompilerMessages messages;

        bool saveTokens;

        public HumphreyParser(IEnumerable<Result<Tokens>> toParse, CompilerMessages overrideDefaultMessages = null)
        {
            saveTokens = false;
            messages = overrideDefaultMessages;
            if (messages==null)
                messages = new CompilerMessages(true, true, false);
            operators = new Stack<(bool binary, int precedance, IOperator item)>(32);
            searchResetQueue = new Queue<Result<Tokens>>(32);
            searchResetBuffer = new Queue<Result<Tokens>>(32);
            operands = new Stack<IAst>(32);
            tokens = toParse.GetEnumerator();
            NextToken();
        }

        bool IsSkippableToken(Tokens token)
        {
            return (token == Tokens.SingleComment || token == Tokens.MultiLineComment);
        }

        void NextToken()
        {
            if (saveTokens)
                searchResetBuffer.Enqueue(lookahead);
            do
            {
                if (searchResetQueue.Count == 0)
                {
                    if (tokens.MoveNext())
                        lookahead = tokens.Current;
                    else
                        lookahead = new Result<Tokens>();
                }
                else
                {
                    lookahead = searchResetQueue.Dequeue();
                }
            } while (lookahead.HasValue && IsSkippableToken(lookahead.Value));
        }

        void SaveTokens()
        {
            saveTokens = true;
        }

        void FlushTokens()
        {
            saveTokens = false;
            searchResetBuffer = new Queue<Result<Tokens>>(32);
        }

        void RestoreTokens()
        {
            saveTokens = false;
            searchResetBuffer.Enqueue(lookahead);
            searchResetQueue = searchResetBuffer;
            searchResetBuffer = new Queue<Result<Tokens>>(32);
            NextToken();
        }

        (bool success, string item, Result<Tokens> token) Item(Tokens kind)
        {
            if (lookahead.HasValue && lookahead.Value == kind)
            {
                var retToken = lookahead;
                var v = lookahead.ToStringValue();
                if (kind == Tokens.Number)
                    v = HumphreyTokeniser.ConvertNumber(v);
                NextToken();
                return (true, v, retToken);
            }
            return (false, "", lookahead);
        }

        Result<Tokens> CurrentToken()
        {
            return lookahead;
        }

        bool Peek(Tokens kind)
        {
            if (lookahead.HasValue && lookahead.Value == kind)
            {
                return true;
            }
            return false;
        }
        bool Take(Tokens kind)
        {
            if (lookahead.HasValue && lookahead.Value == kind)
            {
                NextToken();
                return true;
            }
            return false;
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
            {
                var ast = init(item.item);
                ast.Token = item.token;
                return ast;
            }

            return null;
        }

        // number : Number
        public AstNumber Number() { return AstItem(Tokens.Number, (e) => new AstNumber(e)) as AstNumber; }

        // identifier : Identifier
        public AstIdentifier Identifier() { return AstItem(Tokens.Identifier, (e) => new AstIdentifier(e)) as AstIdentifier; }
        // anonymous : _
        public AstAnonymousIdentifier AnonymousIdentifier() { return AstItem(Tokens.S_Underscore, (e) => new AstAnonymousIdentifier()) as AstAnonymousIdentifier; }
        // identifier : Identifier
        public bool PeekIdentifier() { return Peek(Tokens.Identifier); }
        // loadable_identifier : Identifier
        public AstLoadableIdentifier LoadableIdentifier() { return AstItem(Tokens.Identifier, (e) => new AstLoadableIdentifier(e)) as AstLoadableIdentifier; }

        // number_list : Number*
        public IAst[] NumberList() { return ItemList(Number); }

        // identifer_list : Identifier*        
        public AstIdentifier[] IdentifierList() { return CommaSeperatedItemList<AstIdentifier>(Identifier); }

        // bit_keyword : bit
        public AstBitType BitKeyword() { return AstItem(Tokens.KW_Bit, (e) => new AstBitType()) as AstBitType; }
        public IAst ReturnKeyword() { return AstItem(Tokens.KW_Return, (e) => new AstKeyword(e)); }
        public IAst ForKeyword() { return AstItem(Tokens.KW_For, (e) => new AstKeyword(e)); }
        public IAst IfKeyword() { return AstItem(Tokens.KW_If, (e) => new AstKeyword(e)); }
        public IAst ElseKeyword() { return AstItem(Tokens.KW_Else, (e) => new AstKeyword(e)); }

        // predec : --
        public IAst PreDecrementOperator() { return AstItem(Tokens.O_MinusMinus, (e) => new AstOperator(e)); }
        // preinc : ++
        public IAst PreIncrementOperator() { return AstItem(Tokens.O_PlusPlus, (e) => new AstOperator(e)); }
        // postinc : ++
        public IAst PostIncrementOperator() { return AstItem(Tokens.O_PlusPlus, (e) => new AstOperator(e)); }
        // postdec : --
        public IAst PostDecrementOperator() { return AstItem(Tokens.O_MinusMinus, (e) => new AstOperator(e)); }
        // logical_not : !
        public IAst LogicalNotOperator() { return AstItem(Tokens.O_LogicalNot, (e) => new AstOperator(e)); }
        // binary_not : ~
        public IAst BinaryNotOperator() { return AstItem(Tokens.O_BinaryNot, (e) => new AstOperator(e)); }
        // compare_equal : ==
        public IAst CompareEqualOperator() { return AstItem(Tokens.O_EqualsEquals, (e) => new AstOperator(e)); }
        // compare_equal : !=
        public IAst CompareNotEqualOperator() { return AstItem(Tokens.O_NotEquals, (e) => new AstOperator(e)); }
        // compare_equal : <=
        public IAst CompareLessEqualOperator() { return AstItem(Tokens.O_LessEquals, (e) => new AstOperator(e)); }
        // compare_equal : >=
        public IAst CompareGreaterEqualOperator() { return AstItem(Tokens.O_GreaterEquals, (e) => new AstOperator(e)); }
        // compare_equal : <
        public IAst CompareLessOperator() { return AstItem(Tokens.O_Less, (e) => new AstOperator(e)); }
        // compare_equal : >
        public IAst CompareGreaterOperator() { return AstItem(Tokens.O_Greater, (e) => new AstOperator(e)); }
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
        // logical_and_operator : %
        public IAst LogicalAndOperator() { return AstItem(Tokens.O_LogicalAnd, (e) => new AstOperator(e)); }
        // logical_or_operator : %
        public IAst LogicalOrOperator() { return AstItem(Tokens.O_LogicalOr, (e) => new AstOperator(e)); }
        // binary_and_operator : %
        public IAst BinaryAndOperator() { return AstItem(Tokens.O_BinaryAnd, (e) => new AstOperator(e)); }
        // binary_or_operator : %
        public IAst BinaryOrOperator() { return AstItem(Tokens.O_BinaryOr, (e) => new AstOperator(e)); }
        // binary_xor_operator : %
        public IAst BinaryXorOperator() { return AstItem(Tokens.O_BinaryXor, (e) => new AstOperator(e)); }
        // as_operator : %
        public IAst AsOperator() { return AstItem(Tokens.O_As, (e) => new AstOperator(e)); }
        // reference_operator : .
        public IAst ReferenceOperator() { return AstItem(Tokens.O_Dot, (e) => new AstOperator(e)); }
        // range_operator : .
        public IAst DotDotOperator() { return AstItem(Tokens.O_DotDot, (e) => new AstOperator(e)); }
        // function_call_operator : (
        public IAst FunctionCallOperator() { return AstItem(Tokens.S_OpenParanthesis, (e) => new AstOperator(e)); }
        // function_call_operator : [
        public IAst ArraySubscriptOperator() { return AstItem(Tokens.S_OpenSquareBracket, (e) => new AstOperator(e)); }
        // equals_operator : Equals
        public IAst EqualsOperator() { return AstItem(Tokens.O_Equals, (e) => new AstOperator(e)); }
        public bool PeekEqualsOperator() { return Peek(Tokens.O_Equals); }
        public IAst ColonOperator() { return AstItem(Tokens.O_Colon, (e) => new AstOperator(e)); }
        public bool PeekColonOperator() { return Peek(Tokens.O_Colon); }
        public bool CommaSyntax() { return Take(Tokens.S_Comma); }
        public bool PeekCommaSyntax() { return Peek(Tokens.S_Comma); }
        public bool SemiColonSyntax() { return Take(Tokens.S_SemiColon); }
        public bool OpenParanthesis() { return Take(Tokens.S_OpenParanthesis); }
        public bool PeekOpenParanthesis() { return Peek(Tokens.S_OpenParanthesis); }
        public bool CloseParenthesis() { return Take(Tokens.S_CloseParanthesis); }
        public bool OpenCurlyBrace() { return Take(Tokens.S_OpenCurlyBrace); }
        public bool PeekOpenCurlyBrace() { return Peek(Tokens.S_OpenCurlyBrace); }
        public bool CloseCurlyBrace() { return Take(Tokens.S_CloseCurlyBrace); }
        public bool PeekCloseCurlyBrace() { return Peek(Tokens.S_CloseCurlyBrace); }
        public bool OpenSquareBracket() { return Take(Tokens.S_OpenSquareBracket); }
        public bool PeekOpenSquareBracket() { return Peek(Tokens.S_OpenSquareBracket); }
        public bool CloseSquareBracket() { return Take(Tokens.S_CloseSquareBracket); }
        public bool UnderscoreOperator() { return Take(Tokens.S_Underscore); }
        public bool PointerOperator() { return Take(Tokens.O_Multiply); }

        public AstItemDelegate[] UnaryOperators => new AstItemDelegate[] { AddOperator, SubOperator, MultiplyOperator, LogicalNotOperator, BinaryNotOperator, PreIncrementOperator, PreDecrementOperator };
        public AstItemDelegate[] BinaryOperators => new AstItemDelegate[] { AddOperator, SubOperator, MultiplyOperator, DivideOperator, ModulusOperator, 
                CompareEqualOperator, CompareNotEqualOperator, CompareLessOperator, CompareLessEqualOperator, CompareGreaterOperator, CompareGreaterEqualOperator,
                AsOperator, ReferenceOperator, FunctionCallOperator, ArraySubscriptOperator, PostIncrementOperator, PostDecrementOperator,
                LogicalAndOperator, LogicalOrOperator, BinaryAndOperator, BinaryOrOperator, BinaryXorOperator };
        public AstItemDelegate[] ExpressionKind => new AstItemDelegate[] { UnderscoreExpression, UnaryExpression, BinaryExpression };
        public AstItemDelegate[] BaseTypes => new AstItemDelegate[] { PointerType, ArrayType, BitKeyword, Identifier, FunctionType, StructType };
        public AstItemDelegate[] Types => new AstItemDelegate[] { BaseTypeOrEnumType };
        public AstItemDelegate[] NonFunctionTypes => new AstItemDelegate[] { PointerType, ArrayType, BitKeyword, Identifier, StructType };
        public AstItemDelegate[] IdentifierOrAnonymous => new AstItemDelegate[] { Identifier, AnonymousIdentifier };
        public AstItemDelegate[] Assignables => new AstItemDelegate[] {  CodeBlock, ParseExpression };
        public AstItemDelegate[] Statements => new AstItemDelegate[] { ReturnStatement, ForStatement, IfStatement, CouldBeLocalScopeDefinitionOrAssignmentOrExpression };

        public AstItemDelegate[] StructDefinitions => new AstItemDelegate[] { StructElementDefinition };
        public AstItemDelegate[] EnumDefinitions => new AstItemDelegate[] { EnumElementDefinition };
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

        Stack<(bool binary, int precedance, IOperator item)> operators;
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

        bool IsTopLowerPrecedance(IOperator op, int precedance)
        {
            var peek = operators.Peek();
            int top = peek.precedance;
            int currentOp = precedance;
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
            operators.Push((false, int.MaxValue, null));
        }

        public IExpression PopSentinel()
        {
            if (operators.Pop().item != null)
                return null;
            return operands.Pop() as IExpression;
        }

        public void PushOperator((bool binary, int precedance, IOperator item) op)
        {
            while (IsTopLowerPrecedance(op.item, op.precedance))
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
            IExpression expr = null;
            if (DotDotOperator()==null)
            {
                expr = ParseExpression();
                if (expr == null)
                    return null;

                if (DotDotOperator() == null)
                {
                    if (!CloseSquareBracket())
                        return null;
                    return expr;
                }
            }
            IExpression inclusiveEnd = null;
            if (!CloseSquareBracket())
            {
                inclusiveEnd = ParseExpression();
                if (inclusiveEnd == null)
                    return null;

                if (!CloseSquareBracket())
                    return null;
            }

            if (expr==null && inclusiveEnd==null)
                return null;
                
            return new AstInclusiveRange(expr, inclusiveEnd);
        }

        // ( has already popped at this point
        // function_call_arguments : )
        //                         | expression_list )
        public AstExpressionList FunctionCallArguments()
        {
            AstExpressionList exprList;

            var start = CurrentToken();
            var end = start;
            if (CloseParenthesis())
            {
                exprList = new AstExpressionList();
            }
            else
            {
                exprList = ExpressionList();
                if (exprList == null)
                    return null;
                end = exprList.Token;
                if (!CloseParenthesis())
                    return null;
            }
            exprList.Token = new Result<Tokens>(start.Value, start.Location, end.Remainder);
            return exprList;
        }
        
        // BaseType, or EnumType
        public IType BaseTypeOrEnumType()
        {
            var type = BaseType();
            if (type == null)
                return null;

            if (!PeekOpenCurlyBrace())
                return type;

            return EnumType(type);
        }


        // Used to distinguish between localscope definition or an assignment without requiring significant lookaheads
        public IStatement CouldBeLocalScopeDefinitionOrAssignmentOrExpression()
        {
            if (PeekCloseCurlyBrace())
                return null;

            SaveTokens();

            var localDef = LocalScopeDefinition();
            if (localDef!=null && SemiColonSyntax())
            {
                FlushTokens();
                return localDef;
            }
            RestoreTokens();
            SaveTokens();
            var assign = Assignment();
            if (assign!=null && SemiColonSyntax())
            {
                FlushTokens();
                return assign;
            }
            RestoreTokens();
            SaveTokens();
            var expr = ParseExpression();
            if (expr!=null && SemiColonSyntax())
            {
                FlushTokens();
                var exprStatement = new AstExpressionStatement(expr);
                exprStatement.Token = expr.Token;
                return exprStatement;
            }
            RestoreTokens();
            return null;
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

        // expression_continuation : array_subscript
        //                         | ++
        //                         | --
        public IExpression ExpressionContinuation(IOperator oper)
        {
            switch (oper.Dump())
            {
                case "[":
                    IExpression subscript = ArraySubscript();
                    if (subscript == null)
                        return null;
                    operands.Push(subscript);
                    break;
                case "++":
                case "--":
                    operands.Push(null);
                    break;
                default:
                    throw new System.NotImplementedException($"unhandled {oper.Token} in expression continuation");
            }

            while (operators.Peek().item != null)
                PopOperator();
            var op = OneOf(BinaryOperators) as IOperator;
            var expr = operands.Peek() as IExpression;
            if (op!=null)
            {
                return BinaryOperatorProcess(expr, op);
            }
            return expr;
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

        public AstAssignmentStatement Assignment()
        {
            var exprList = ExpressionList();
            if (exprList == null)
                return null;
            if (EqualsOperator()==null)
                return null;
            var assignable = Assignable();
            if (assignable == null)
                return null;

            var assign = new AstAssignmentStatement(exprList, assignable);
            assign.Token = new Result<Tokens>(exprList.Token.Value, exprList.Token.Location, assignable.Token.Remainder);
            return assign;
        }

        public AstRange Range()
        {
            var inclusiveBegin = ParseExpression();
            if (inclusiveBegin == null)
                return null;

            if (DotDotOperator() == null)
                return null;

            var exclusiveEnd = ParseExpression();
            if (exclusiveEnd == null)
                return null;

            var range = new AstRange(inclusiveBegin, exclusiveEnd);
            range.Token = new Result<Tokens>(inclusiveBegin.Token.Value, inclusiveBegin.Token.Location, exclusiveEnd.Token.Remainder);
            return range;
        }

        public AstIfStatement IfStatement()
        {
            var start = CurrentToken();
            if (IfKeyword() == null)
                return null;

            var expression = ParseExpression();
            if (expression == null)
                return null;

            var codeBlock = CodeBlock();
            if (codeBlock == null)
                return null;

            var end = codeBlock.Token;

            AstIfStatement statement = default;
            if (ElseKeyword() != null)
            {
                var elseCodeBlock = CodeBlock();
                if (elseCodeBlock == null)
                    return null;

                end = elseCodeBlock.Token;
                statement = new AstIfStatement(expression, codeBlock, elseCodeBlock);
            }
            else
            {
                statement = new AstIfStatement(expression, codeBlock, null);
            }
            statement.Token = new Result<Tokens>(start.Value, start.Location, end.Remainder);
            return statement;
        }

        public AstForStatement ForStatement()
        {
            var start = CurrentToken();

            if (ForKeyword() == null)
                return null;

            var identifierList = CommaSeperatedItemList<AstLoadableIdentifier>(LoadableIdentifier);
            if (identifierList == null)
                return null;

            if (EqualsOperator()==null)
                return null;

            // Todo we should allow ranges or expressions (e.g. for x = mycollection )
            var rangeList = CommaSeperatedItemList<AstRange>(Range);
            if (rangeList == null)
                return null;

            var codeBlock = CodeBlock();
            if (codeBlock == null)
                return null;

            var forStatement = new AstForStatement(identifierList, rangeList, codeBlock);
            forStatement.Token = new Result<Tokens>(start.Value, start.Location, codeBlock.Token.Remainder);
            return forStatement;
        }

        public IExpression BinaryOperatorProcess(IExpression terminal, IOperator op)
        {
            PushOperator((true, op.BinaryPrecedance, op));
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
                    return ExpressionContinuation(op);
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
            var start = CurrentToken();
            if (!UnderscoreOperator())
                return null;

            var operand = new AstUnderscoreExpression();
            operand.Token = start;
            operands.Push(operand);
            return operand;
        }

        // unary_expression : unary_operator expression
        public IExpression UnaryExpression()
        {
            var op = OneOf(UnaryOperators) as IOperator;
            if (op == null)
                return null;
            PushOperator((false, op.UnaryPrecedance, op));
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

            var paramDefinition = new AstParamDefinition(identifier, typeSpecifier);
            paramDefinition.Token = new Result<Tokens>(identifier.Token.Value, identifier.Token.Location, typeSpecifier.Token.Remainder);
            return paramDefinition;
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
            var list = new AstExpressionList(exprList);
            var last = exprList[exprList.Length - 1].Token;
            list.Token = new Result<Tokens>(exprList[0].Token.Value, exprList[0].Token.Location, last.Remainder);
            return list;
        }

        // parameter_list : ( param_definition_list )
        //                | ( )
        public AstParamList ParamList()
        {
            var start = CurrentToken();

            if (!OpenParanthesis())
                return null;

            var end = CurrentToken();
            if (CloseParenthesis())
            {
                var emptyList = new AstParamList(new AstParamDefinition[] { });
                emptyList.Token = new Result<Tokens>(start.Value, start.Location, end.Remainder);
                return emptyList;
            }

            var paramDefinitionList = ParamDefinitionList();
            if (paramDefinitionList == null)
                return null;

            end = CurrentToken();
            if (!CloseParenthesis())
                return null;

            var paramList = new AstParamList(paramDefinitionList);
            paramList.Token = new Result<Tokens>(start.Value, start.Location, end.Remainder);
            return paramList;
        }

        // pointer_type : * bit|identifier|functionType|structType
        public AstPointerType PointerType()
        {
            var start = CurrentToken();

            if (!PointerOperator())
                return null;

            var typeSpecifier = OneOf(NonFunctionTypes);
            if (typeSpecifier == null)
                return null;

            var pointerType = new AstPointerType(typeSpecifier as IType);
            pointerType.Token = new Result<Tokens>(start.Value, start.Location, typeSpecifier.Token.Remainder);
            return pointerType;
        }

        // array_type : [ConstantExpr] bit|identifier|functionType|structType
        public AstArrayType ArrayType()
        {
            var start = CurrentToken();

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

            var arrayType = new AstArrayType(expr, typeSpecifier as IType);
            arrayType.Token = new Result<Tokens>(start.Value, start.Location, typeSpecifier.Token.Remainder);
            return arrayType;
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

            var functionType = new AstFunctionType(inputs, outputs);
            functionType.Token = new Result<Tokens>(inputs.Token.Value, inputs.Token.Location, outputs.Token.Remainder);
            return functionType;
        }
        
        // enum_type : type { enum_element }
        public AstEnumType EnumType(IType type)
        {
            var start = type.Token;

            if (!OpenCurlyBrace())
                return null;

            var definitionList = ManyOf<AstEnumElement>(EnumDefinitions);
            if (definitionList == null)
                return null;

            var end = CurrentToken();

            if (!CloseCurlyBrace())
                return null;

            var enumType = new AstEnumType(type, definitionList);
            enumType.Token = new Result<Tokens>(start.Value, start.Location, end.Remainder);
            return enumType;
        }

        // struct_type : { struct_element* }
        public AstStructureType StructType()
        {
            var start = CurrentToken();

            if (!OpenCurlyBrace())
                return null;

            var definitionList = ManyOf<AstStructElement>(StructDefinitions);
            if (definitionList==null)
                return null;

            var end = CurrentToken();

            if (!CloseCurlyBrace())
                return null;

            var structType = new AstStructureType(definitionList);
            structType.Token = new Result<Tokens>(start.Value, start.Location, end.Remainder);
            return structType;
        }

        // type : bit                       // builtin
        //      | identifier                // type
        //      | struct_type               // struct
        //      | function_type             // function
        public IType BaseType() { return OneOf(BaseTypes) as IType; }

        // type : base_type or enum type
        public IType Type() { return OneOf(Types) as IType; }
        public IType NonFunctionType() { return OneOf(NonFunctionTypes) as IType; }

        public IIdentifier IdentifierOrAnon() { return OneOf(IdentifierOrAnonymous) as IIdentifier; }

        // assignable : { statements }      // function body
        //            | expression
        public IAssignable Assignable() { return OneOf(Assignables) as IAssignable; }

        // enum_element_definition : identifier := assignable
        //                           
        public AstEnumElement EnumElementDefinition()
        {
            var (ok, identifierList, typeSpecifier, assignable, token) = Definition<AstIdentifier>(Identifier, NonFunctionType, Assignable);
            if (!ok)
                return null;
            if (typeSpecifier!=null || assignable==null)
                throw new System.NotImplementedException($"TODO - This should be an error, enums must be of the format identifier:= assignable");

            var enumElement = new AstEnumElement(identifierList, assignable);
            enumElement.Token = token;
            return enumElement;
        }

        // struct_element_definition : identifier : non_function_type
        //                           | identifier := assignable
        //                           | identifier : non_function_type = assignable
        public AstStructElement StructElementDefinition()
        {
            var (ok, identifierList, typeSpecifier, assignable, token) = Definition<IIdentifier>(IdentifierOrAnon, NonFunctionType, Assignable);
            if (!ok)
                return null;
            var structElement = new AstStructElement(identifierList, typeSpecifier, assignable);
            structElement.Token = token;
            return structElement;
        }

        // global_definition : identifier : non_function_type
        //                   | identifier := assignable
        //                   | identifier : non_function_type = assignable
        public AstGlobalDefinition GlobalScopeDefinition()
        {
            var (ok, identifierList, typeSpecifier, assignable, token) = Definition<AstIdentifier>(Identifier, Type, Assignable);
            if (!ok)
                return null;
            var globalDef = new AstGlobalDefinition(identifierList, typeSpecifier, assignable);
            globalDef.Token = token;
            return globalDef;
        }

        // local_definition  : identifier : non_function_type
        //                   | identifier := assignable
        //                   | identifier : non_function_type = assignable
        public AstLocalDefinition LocalScopeDefinition()
        {
            var (ok, identifierList, typeSpecifier, assignable, token) = Definition<AstIdentifier>(Identifier, Type, Assignable);
            if (!ok)
                return null;
            var localDef = new AstLocalDefinition(identifierList, typeSpecifier, assignable);
            localDef.Token = token;
            return localDef;
        }

        // definition : identifier : type
        //            | identifier := assignable
        //            | identifier : type = assignable
        public (bool ok, T[] identifierList,IType typeSpecifier, IAssignable assignable, Result<Tokens> token) Definition<T>(AstItemDelegate identifierDelegate,AstItemDelegate typeDelegate, AstItemDelegate assignableDelegate) where T : class
        {
            var start = CurrentToken();

            var identifier = CommaSeperatedItemList<T>(identifierDelegate);
            if (identifier == null)
                return (false, null, null, null, new Result<Tokens>());

            IType typeSpecifier = null;
            IAssignable assignable = null;

            if (ColonOperator() == null)
                return (false, null, null, null, new Result<Tokens>());

            typeSpecifier = typeDelegate() as IType;

            TokenSpan end = new TokenSpan();

            if (typeSpecifier!=null)
                end = typeSpecifier.Token.Remainder;

            if (assignableDelegate != null)
            {
                if (EqualsOperator() != null)
                {
                    assignable = assignableDelegate() as IAssignable;
                    if (assignable == null)
                        return (false, null, null, null, new Result<Tokens>());

                    end = assignable.Token.Remainder;
                }
            }

            if (typeSpecifier==null && assignable==null)
                return (false, null, null, null, new Result<Tokens>());

            return (true, identifier, typeSpecifier, assignable, new Result<Tokens>(start.Value,start.Location, end));
        }

        // return_statement : return 
        public AstReturnStatement ReturnStatement()
        {
            var start = CurrentToken();
            if (ReturnKeyword() == null)
                return null;

            if (!SemiColonSyntax())
                return null;

            var returnStatement = new AstReturnStatement();
            returnStatement.Token = start;
            return returnStatement;
        }

        // statement : block
        //           | keyword..
        //           | assignment
        public IStatement Statement() 
        {
            var codeBlock = CodeBlock();
            if (codeBlock!=null)
                return codeBlock;
            var statement=OneOf(Statements) as IStatement;
            return statement;
        }

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
            var start = CurrentToken();

            if (!OpenCurlyBrace())
                return null;

            var statements = StatementList();
            if (statements == null)
                return null;

            var end = CurrentToken();
            if (!CloseCurlyBrace())
                return null;

            var codeBlock = new AstCodeBlock(statements);
            codeBlock.Token = new Result<Tokens>(start.Value, start.Location, end.Remainder);
            return codeBlock;
        }

    }
}
