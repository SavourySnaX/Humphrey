using Humphrey.Backend;
using Humphrey.FrontEnd;

public interface ICompilationConstantValue : ICompilationValue
{
    public void Cast(IType type);
    CompilationValue GetCompilationValue(CompilationUnit unit, CompilationType destType);
}
