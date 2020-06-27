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
                    case "*":
                        return 300;
                    case "/":
                        return 300;
                    case "=":
                        return 150;
                    case ":":
                        return 100;
                    default:
                        throw new ParseException($"Unimplemented Precadnce for operator : {temp}");
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
