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
                    case "+":
                        return 500;
                    case "-":
                        return 500;
                    case "%":
                        return 300;
                    case "*":
                        return 300;
                    case "/":
                        return 300;
                    case "=":
                        return 150;
                    case ":":
                        return 100;
                    case "as":
                        return 900;
                    case ".":
                        return 1000;
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
                    default:
                        return IOperator.OperatorKind.ExpressionExpression;
                }
            }
        }

        public string Dump()
        {
            return temp;
        }
    }
}
