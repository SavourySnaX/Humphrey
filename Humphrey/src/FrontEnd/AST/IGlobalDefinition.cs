using Humphrey.Backend;

namespace Humphrey.FrontEnd
{

    public interface IGlobalDefinition : IAst
    {
        bool Compile(CompilationUnit unit);
    }
}
