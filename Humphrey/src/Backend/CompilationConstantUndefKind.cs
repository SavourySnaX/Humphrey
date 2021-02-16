
using Humphrey.FrontEnd;
using System.Numerics;

using Extensions;

namespace Humphrey.Backend
{
    public class CompilationConstantUndefKind : ICompilationConstantValue
    {
        IType resultType;
        SourceLocation location;

        Result<Tokens> frontendLocation;

        public CompilationConstantUndefKind(Result<Tokens> frontendLoc)
        {
            resultType = null;
            location = new SourceLocation(frontendLoc);
            frontendLocation = frontendLoc;
        }

        public bool Same(CompilationConstantUndefKind other)
        {
            return true;
        }

        public CompilationValue GetCompilationValue(CompilationUnit unit, CompilationType destType)
        {
            if (destType == null && resultType != null)
                destType = resultType.CreateOrFetchType(unit).compilationType;

            if (destType == null)
            {
                throw new System.NotImplementedException("Invalid");
            }

            return unit.CreateUndef(destType);
        }

        public void Cast(IType type)
        {
            resultType = type;
        }

        public Result<Tokens> FrontendLocation => frontendLocation;
    }
}

