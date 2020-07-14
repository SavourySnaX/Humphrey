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
                    default:
                        throw new ParseException($"Unimplemented Precadence for operator : {temp}");
                }
            }
        }

        public bool RhsType
        {
            get
            {
                switch (temp)
                {
                    case "as":
                        return true;
                    default:
                        return false;
                }
            }
        }

    
        public bool Compile(CompilationUnit unit)
        {
            return false;
        }
    
        public string Dump()
        {
            return temp;
        }
    }
}
