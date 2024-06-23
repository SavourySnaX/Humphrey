using Humphrey.Backend;
using Humphrey.FrontEnd;

public interface ICompilationConstantValue : ICompilationValue
{
    public ICompilationConstantValue Cast(IType type);
    CompilationValue GetCompilationValue(CompilationUnit unit, CompilationType destType);
}
