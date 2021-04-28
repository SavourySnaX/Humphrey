
using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstUsingNamespace : IGlobalDefinition
    {
        IIdentifier _toInclude;
        AstIdentifier _newName;
        public AstUsingNamespace(IIdentifier toInclude,AstIdentifier newName)
        {
            _toInclude = toInclude;
            _newName = newName;

        }

        public bool Compile(CompilationUnit unit)
        {
            foreach (var e in toCompile)
            {
                e.Compile(unit);
            }

            return true;
        }

        public string Dump()
        {
            if (_newName!=null)
                return $"using {_toInclude.Dump()} as {_newName.Dump()}";
            return $"using {_toInclude.Dump()}";
        }

        public void Semantic(SemanticPass pass)
        {
            if (_toInclude is AstIdentifier identifier)
            {
                toCompile = pass.ImportNamespace(new IIdentifier[] {identifier});
                return;
            }
            var namesp = _toInclude as AstNamespaceIdentifier;
            toCompile = pass.ImportNamespace(namesp.FullPath);
        }

        private IGlobalDefinition[] toCompile;

        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

        public AstIdentifier[] Identifiers => null;

    }
}