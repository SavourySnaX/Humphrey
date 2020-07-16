
using Humphrey.FrontEnd;
using System.Numerics;

using Extensions;

namespace Humphrey.Backend
{
    public class CompilationConstantValue : ICompilationValue
    {
        BigInteger constant;
        bool undefValue;
        IType resultType;

        public CompilationConstantValue()
        {
            undefValue = true;
            constant = BigInteger.Zero;
            resultType = null;
        }
        
        public CompilationConstantValue(AstNumber val)
        {
            constant = BigInteger.Parse(val.Dump());
            undefValue = false;
        }

        public (uint numBits, bool isSigned) ComputeKind()
        {
            var ival = constant;

            uint numBits = 0;
            int sign = ival.Sign;
            switch (sign)
            {
                case -1:
                    numBits++;
                    goto case 1;
                case 1:
                    var tVal = ival;
                    if (sign==-1)
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

            return (numBits, sign==-1);
        }

        public CompilationValue GetCompilationValue(CompilationUnit unit, CompilationType destType)
        {
            if (undefValue)
                return unit.CreateUndef(destType);

            var (numBits, isSigned) = ComputeKind();

            if (destType==null)
                return unit.CreateConstant(this, numBits, isSigned);

            if (destType.IsIntegerType)
            {
                if (numBits < destType.IntegerWidth)
                {
                    return unit.CreateConstant(this, destType.IntegerWidth, destType.IsSigned);
                }
                else if (numBits == destType.IntegerWidth)
                {
                    if (isSigned == destType.IsSigned)
                    {
                        return unit.CreateConstant(this, numBits, isSigned);
                    }
                    throw new System.NotImplementedException($"TODO - signed/unsigned mismatch");
                }
                throw new System.NotImplementedException($"TODO - Integer Bit width does not match");
            }
            else if (destType.IsPointer)
            {
                var type = resultType.CreateOrFetchType(unit);
                if (type != destType)
                {
                    // Create the constant
                    var constant = unit.CreateConstant(this, numBits, isSigned);
                    return new CompilationValue(constant.BackendValue.ConstIntToPtr(type.BackendType), type);
                }
            }
            throw new System.NotImplementedException($"TODO - Non integer types in promotion?");
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
        public void Cast(IType type)
        {
            resultType = type;
        }
        public BigInteger Constant => constant;
    }
}

