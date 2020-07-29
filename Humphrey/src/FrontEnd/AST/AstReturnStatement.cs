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
            builder.BackendValue.BuildRetVoid();

            return true;
        }

        public string Dump()
        {
            return "return";
        }
    }
}

