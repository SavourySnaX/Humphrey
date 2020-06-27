using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public interface IStatement : IAst
    {
        bool BuildStatement(CompilationUnit unit, CompilationFunction function, CompilationBuilder builder);
    }
}
