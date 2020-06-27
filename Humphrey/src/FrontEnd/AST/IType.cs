using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public interface IType : IAst
    {
        CompilationType CreateOrFetchType(CompilationUnit unit);

        bool IsFunctionType { get; }
    }
}
