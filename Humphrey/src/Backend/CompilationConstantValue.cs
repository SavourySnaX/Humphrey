using LLVMSharp.Interop;

using Humphrey.FrontEnd;
using System.Numerics;

namespace Humphrey.Backend
{
    public class CompilationConstantValue
    {
        BigInteger constant;

        public CompilationConstantValue(AstNumber val)
        {
            constant = BigInteger.Parse(val.Dump());
        }

        public void Negate()
        {
            constant = 0 - constant;
        }

        public void Add(CompilationConstantValue rhs)
        {
            constant = constant + rhs.Constant;
        }
        public void Sub(CompilationConstantValue rhs)
        {
            constant = constant - rhs.Constant;
        }
        public void Mul(CompilationConstantValue rhs)
        {
            constant = constant * rhs.Constant;
        }
        public void Div(CompilationConstantValue rhs)
        {
            constant = constant / rhs.Constant;
        }
        public void Rem(CompilationConstantValue rhs)
        {
            constant = constant % rhs.Constant;
        }
        
        public BigInteger Constant => constant;
    }
}

