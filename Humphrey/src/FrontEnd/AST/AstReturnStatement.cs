using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public class AstReturnStatement : IStatement
    {
        public AstReturnStatement()
        {
        }

        public bool BuildStatement(CompilationUnit unit, CompilationFunction function, CompilationBuilder builder)
        {
            builder.SetDebugLocation(unit, new SourceLocation(Token));

            builder.Branch(function.ExitBlock);

            return true;
        }

        public string Dump()
        {
            return "return";
        }
        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}

