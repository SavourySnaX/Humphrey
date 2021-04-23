
using Humphrey.FrontEnd;
using System.Numerics;

using Extensions;

namespace Humphrey.Backend
{
    public class CompilationConstantIntegerKind : ICompilationConstantValue
    {
        BigInteger constant;
        IType resultType;
        SourceLocation location;

        Result<Tokens> frontendLocation;

        public bool Same(CompilationConstantIntegerKind other)
        {
            return constant==other.constant;
        }

        public CompilationConstantIntegerKind(AstNumber val)
        {
            constant = BigInteger.Parse(val.Dump());
            frontendLocation = val.Token;
            location = new SourceLocation(frontendLocation);
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

            return (numBits, sign == -1);
        }

        public CompilationValue GetCompilationValue(CompilationUnit unit, CompilationType destType)
        {
            var (numBits, isSigned) = ComputeKind();

            if (destType == null && resultType != null)
                destType = resultType.CreateOrFetchType(unit).compilationType;

            if (destType == null)
                return unit.CreateConstant(this, numBits, isSigned, location);

            // Special cases for 0 (which we allow for use in stating zero initialisation for complex types)
            // disallow for pointers though as we don't want implicit conversion
            if (constant.IsZero && !(destType is CompilationPointerType))
                return unit.CreateZero(destType);

            if (destType is CompilationEnumType compilationEnumType)
                destType = compilationEnumType.ElementType;

            if (destType is CompilationIntegerType destIntType)
            {
                if (numBits < destIntType.IntegerWidth)
                {
                    return unit.CreateConstant(this, destIntType.IntegerWidth, destIntType.IsSigned, location);
                }
                else if (numBits == destIntType.IntegerWidth)
                {
                    if (isSigned == destIntType.IsSigned)
                    {
                        return unit.CreateConstant(this, numBits, isSigned, location);
                    }
                    throw new System.NotImplementedException($"TODO - signed/unsigned mismatch");
                }
                unit.Messages.Log(CompilerErrorKind.Error_IntegerWidthMismatch, $"Constant '{FrontendLocation.Location.ToStringValue(FrontendLocation.Remainder)}' is larger than {destIntType.DumpType()}!", FrontendLocation.Location, FrontendLocation.Remainder);
                return unit.CreateUndef(destType);  // Allow compilation to continue
            }
            else if (destType is CompilationPointerType destPtrType)
            {
                CompilationType type;
                if (resultType == null)
                {
                    unit.Messages.Log(CompilerErrorKind.Error_TypeMismatch, $"Attempting to assign {constant} to a pointer type {destType.DumpType()}, you must supply a destination pointer type via as.", frontendLocation.Location, frontendLocation.Remainder);
                    type = destType;    // Attempt recovery from error
                }
                else
                    type = resultType.CreateOrFetchType(unit).compilationType;

                if (!type.Same(destType))
                {
                    unit.Messages.Log(CompilerErrorKind.Error_TypeMismatch, $"Attempting to assign a value of type '{type.DumpType()}' to type '{destType.DumpType()}.'", frontendLocation.Location, frontendLocation.Remainder);
                    type = destType;    // Attempt recovery from error
                }
                if (type.Same(destType))
                {
                    // Create the constant
                    var constant = unit.CreateConstant(this, numBits, isSigned, location);
                    return new CompilationValue(constant.BackendValue.ConstIntToPtr(type.BackendType), type, FrontendLocation);
                }
            }

            throw new System.NotImplementedException($"TODO - Non integer types in promotion?");
        }

        public void Negate()
        {
            constant = 0 - constant;
        }

        public void Increment()
        {
            constant = constant + 1;
        }

        public void Decrement()
        {
            constant = constant - 1;
        }

        public void Add(CompilationConstantIntegerKind rhs)
        {
            constant = constant + rhs.Constant;
        }
        public void Sub(CompilationConstantIntegerKind rhs)
        {
            constant = constant - rhs.Constant;
        }
        public void Mul(CompilationConstantIntegerKind rhs)
        {
            constant = constant * rhs.Constant;
        }
        public void Div(CompilationConstantIntegerKind rhs)
        {
            constant = constant / rhs.Constant;
        }
        public void Rem(CompilationConstantIntegerKind rhs)
        {
            constant = constant % rhs.Constant;
        }
        public void CompareLess(CompilationConstantIntegerKind rhs)
        {
            constant = (constant < rhs.Constant) ? BigInteger.One : BigInteger.Zero;
        }
        public void CompareLessEqual(CompilationConstantIntegerKind rhs)
        {
            constant = (constant <= rhs.Constant) ? BigInteger.One : BigInteger.Zero;
        }
        public void CompareGreater(CompilationConstantIntegerKind rhs)
        {
            constant = (constant > rhs.Constant) ? BigInteger.One : BigInteger.Zero;
        }
        public void CompareGreaterEqual(CompilationConstantIntegerKind rhs)
        {
            constant = (constant >= rhs.Constant) ? BigInteger.One : BigInteger.Zero;
        }
        public void CompareEqual(CompilationConstantIntegerKind rhs)
        {
            constant = (constant == rhs.Constant) ? BigInteger.One : BigInteger.Zero;
        }
        public void CompareNotEqual(CompilationConstantIntegerKind rhs)
        {
            constant = (constant != rhs.Constant) ? BigInteger.One : BigInteger.Zero;
        }

        public void LogicalOr(CompilationConstantIntegerKind rhs)
        {
            constant = (constant.IsOne || rhs.Constant.IsOne) ? BigInteger.One : BigInteger.Zero;
        }

        public void LogicalAnd(CompilationConstantIntegerKind rhs)
        {
            constant = (constant.IsOne && rhs.Constant.IsOne) ? BigInteger.One : BigInteger.Zero;
        }

        public void LogicalNot()
        {
            constant = constant.IsOne ? BigInteger.One : BigInteger.Zero;
        }

        public void LogicalShiftLeft(CompilationConstantIntegerKind rhs)
        {
            var tConstant = rhs.constant;
            if (tConstant>=0)
            {
                while (tConstant > 0)
                {
                    constant <<= 1;
                    tConstant--;
                }
            }
            else
            {
                var kind = ComputeKind();
                while (tConstant < 0)
                    tConstant += kind.numBits;
                tConstant = tConstant % kind.numBits;
                while (tConstant > BigInteger.Zero)
                {
                    constant = constant << 1;
                    tConstant -= 1;
                }
            }
        }

        public void LogicalShiftRight(CompilationConstantIntegerKind rhs)
        {
            var kind = ComputeKind();
            var tConstant = rhs.constant;
            while (tConstant<0)
                tConstant += kind.numBits;
            tConstant = tConstant % kind.numBits;
            var maskTopBit = (1<<(int)(kind.numBits-1))-1;
            while (tConstant > BigInteger.Zero)
            {
                constant = constant >> 1;
                constant &= maskTopBit;   // clear top bit
                tConstant -= 1;
            }
        }

        public void ArithmeticShiftRight(CompilationConstantIntegerKind rhs)
        {
            var kind = ComputeKind();
            var tConstant = rhs.constant;
            while (tConstant<0)
                tConstant += kind.numBits;
            tConstant = tConstant % kind.numBits;
            var maskTopBit = (1<<(int)(kind.numBits-1))-1;
            var setTopBit = 1<<(int)(kind.numBits-1) & constant;
            while (tConstant > BigInteger.Zero)
            {
                constant = constant >> 1;
                constant &= maskTopBit;     // clear top bit
                constant |= setTopBit;      // or in arithmetic bit
                tConstant -= 1;
            }
        }

        public void Not()
        {
            constant = ~Constant;
        }

        public void And(CompilationConstantIntegerKind rhs)
        {
            constant = constant & rhs.Constant;
        }

        public void Xor(CompilationConstantIntegerKind rhs)
        {
            constant = constant ^ rhs.Constant;
        }

        public void Or(CompilationConstantIntegerKind rhs)
        {
            constant = constant | rhs.Constant;
        }

        public void Cast(IType type)
        {
            resultType = type;
        }

        public BigInteger Constant => constant;

        public Result<Tokens> FrontendLocation => frontendLocation;
    }
}

