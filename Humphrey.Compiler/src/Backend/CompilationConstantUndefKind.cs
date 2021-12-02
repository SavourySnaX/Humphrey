using Humphrey.FrontEnd;

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
                unit.Messages.Log(CompilerErrorKind.Error_UndefinedType, $"An Undef value '_' cannot be used in an expression that requires a type", FrontendLocation.Location, FrontendLocation.Remainder);
                throw new CompilationAbortException();
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

