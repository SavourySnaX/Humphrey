using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public interface IStatement : IAst
    {
        void Semantic(SemanticPass pass);
        bool BuildStatement(CompilationUnit unit, CompilationFunction function, CompilationBuilder builder);
    }
}
