using System.Numerics;
using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstNumber : IExpression
    {
        string temp;
        public AstNumber(string value)
        {
            temp = value;
        }
    
        public string Dump()
        {
            return temp;
        }
        public ICompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            return new CompilationConstantIntegerKind(this);
        }

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            return new CompilationConstantIntegerKind(this);
        }

        public IType ResolveExpressionType(SemanticPass pass)
        {
            var ival = BigInteger.Parse(temp);
            uint numBits = 0;
            int sign = ival.Sign;
            switch (sign)
            {
                case -1:
                    numBits++;
                    goto case 1;
                case 1:
                    var tVal = ival;
                    if (sign == -1)
                        tVal *= -1;

                    while (tVal != BigInteger.Zero)
                    {
                        tVal /= 2;
                        numBits++;
                    }

                    break;
                case 0:
                    numBits = 1;
                    break;

            }
            if (numBits==1)
                return new AstBitType();
            return new AstArrayType(new AstNumber($"{numBits}"), new AstBitType());
        }

        public void Semantic(SemanticPass pass)
        {
            // Nothing to do
        }

        private Result<Tokens> _token;

        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}