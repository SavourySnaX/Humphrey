using Humphrey.Backend;

namespace Humphrey.FrontEnd
{

    public interface IGlobalDefinition : IAst
    {
        void Semantic(SemanticPass pass);
        bool Compile(CompilationUnit unit);

        AstIdentifier[] Identifiers { get; }
    }
}
