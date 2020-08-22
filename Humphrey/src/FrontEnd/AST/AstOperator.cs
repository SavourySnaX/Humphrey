using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstOperator : IOperator
    {
        string temp;
        public AstOperator(string value)
        {
            temp = value;
        }

        public int Precedance 
        {
            get
            {
                switch (temp)
                {
                    case "":
                        return int.MaxValue;
                    case "as":
                        return 1000;
                    case "~":
                    case "!":
                        return 850;
                    case "||":
                        return 775;
                    case "&&":
                        return 750;
                    case "|":
                        return 675;
                    case "^":
                        return 665;
                    case "&":
                        return 650;
                    case "==":
                    case "!=":
                        return 625;
                    case "<":
                    case ">":
                    case "<=":
                    case ">=":
                        return 600;
                    case "+":
                    case "-":
                        return 500;
                    case "%":
                    case "*":
                    case "/":
                        return 300;
                    case "=":
                        return 150;
                    case ":":
                    case ".":
                    case "(":
                    case "[":
                        return 100;
                    default:
                        throw new ParseException($"Unimplemented Precadence for operator : {temp}");
                }
            }
        }

        public IOperator.OperatorKind RhsKind
        {
            get
            {
                switch (temp)
                {
                    case "as":
                        return IOperator.OperatorKind.ExpressionType;
                    case ".":
                        return IOperator.OperatorKind.ExpressionIdentifier;
                    case "(":
                        return IOperator.OperatorKind.ExpressionExpressionList;
                    case "[":
                        return IOperator.OperatorKind.ExpressionExpressionContinuation;
                    default:
                        return IOperator.OperatorKind.ExpressionExpression;
                }
            }
        }

        public string Dump()
        {
            return temp;
        }
        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}