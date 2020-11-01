using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public interface IType : IAst
    {
        (CompilationType compilationType,IType originalType) CreateOrFetchType(CompilationUnit unit);

        bool IsFunctionType { get; }
        AstMetaData MetaData { get; set; }
    }
}
