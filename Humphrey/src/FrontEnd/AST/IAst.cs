using Humphrey.Backend;

namespace Humphrey.FrontEnd
{

    public interface IAst
    {
        bool Compile(CompilationUnit unit);
        string Dump();
    }
}