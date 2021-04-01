

using Humphrey.FrontEnd;

namespace Humphrey.Backend
{
    public class CompilationConstantArrayKind : ICompilationConstantValue
    {
        IType element;
        ICompilationConstantValue[] values;
        SourceLocation location;
        Result<Tokens> frontendLocation;

        public CompilationConstantArrayKind(IType elementType, ICompilationConstantValue[] initialiser, Result<Tokens> frontendLoc)
        {
            element = elementType;
            values = initialiser;
            location = new SourceLocation(frontendLoc);
            frontendLocation = frontendLoc;
        }

        public void Cast(IType type)
        {
            throw new System.NotImplementedException();
        }

        public CompilationValue GetCompilationValue(CompilationUnit unit, CompilationType destType)
        {
            var arrayType = new AstArrayType(new AstNumber($"{values.Length}"), element);
            arrayType.Token = frontendLocation;
            var compiledArrayType = arrayType.CreateOrFetchType(unit).compilationType;
            if (destType != null)
            {
                if (!destType.Same(compiledArrayType))
                {
                    unit.Messages.Log(CompilerErrorKind.Error_TypeMismatch, $"Result of expression '{frontendLocation.Location.ToStringValue(frontendLocation.Remainder)}' of type '{compiledArrayType.DumpType()}' does not match destination type '{destType.DumpType()}'!", frontendLocation.Location, frontendLocation.Remainder);
                    return unit.CreateUndef(destType);  // Allow compilation to continue
                }
            }
            var constArray = unit.CreateConstantArray(values, element.CreateOrFetchType(unit).compilationType);
            return new CompilationValue(constArray, compiledArrayType, frontendLocation);
        }

        public Result<Tokens> FrontendLocation => frontendLocation;
    }
}