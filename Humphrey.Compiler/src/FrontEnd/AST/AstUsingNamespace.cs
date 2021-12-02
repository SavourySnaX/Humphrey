
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
                pass.ImportNamespace(new IIdentifier[] {identifier});
            }
            else
            {
                var namesp = _toInclude as AstNamespaceIdentifier;
                pass.ImportNamespace(namesp.FullPath);
            }
        }

        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

        public AstIdentifier[] Identifiers => null;

    }
}