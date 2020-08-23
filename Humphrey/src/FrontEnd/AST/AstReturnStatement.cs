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
            builder.BackendValue.BuildBr(function.ExitBlock.BackendValue);

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

