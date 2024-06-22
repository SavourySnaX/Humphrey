
using Humphrey.FrontEnd;
using System.Numerics;

using Extensions;

namespace Humphrey.Backend
{
    public class CompilationConstantFloatKind : ICompilationConstantValue
    {
        float constant;
        IType resultType;
        SourceLocation location;

        Result<Tokens> frontendLocation;

        public bool Same(CompilationConstantFloatKind other)
        {
            return constant==other.constant;
        }

        public CompilationConstantFloatKind(AstFloatNumber val)
        {
            constant = float.Parse(val.Dump());
            frontendLocation = val.Token;
            location = new SourceLocation(frontendLocation);
        }

        private (uint numBits, bool isSigned) ComputeKind()
        {
            return (32, true);
        }

        public CompilationValue GetCompilationValue(CompilationUnit unit, CompilationType destType)
        {
            var (numBits, isSigned) = ComputeKind();

            if (destType == null && resultType != null)
                destType = resultType.CreateOrFetchType(unit).compilationType;

            if (destType == null)
                return unit.CreateConstant(this, location);

            if (destType is CompilationFloatType destFloatType)
            {
                return unit.CreateConstant(this, location);
            }

            unit.Messages.Log(CompilerErrorKind.Error_TypeMismatch, $"Attempting to assign a value '{constant}' to type '{destType.DumpType()}.'", frontendLocation.Location, frontendLocation.Remainder);
			return unit.CreateUndef(destType);
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

        public void Add(CompilationConstantFloatKind rhs)
        {
            constant = constant + rhs.Constant;
        }
        public void Sub(CompilationConstantFloatKind rhs)
        {
            constant = constant - rhs.Constant;
        }
        public void Mul(CompilationConstantFloatKind rhs)
        {
            constant = constant * rhs.Constant;
        }
        public void Div(CompilationConstantFloatKind rhs)
        {
            constant = constant / rhs.Constant;
        }
        public void Rem(CompilationConstantFloatKind rhs)
        {
            constant = constant % rhs.Constant;
        }

        public void Cast(IType type)
        {
            resultType = type;
        }

        public float Constant => constant;

        public Result<Tokens> FrontendLocation => frontendLocation;
    }
}


