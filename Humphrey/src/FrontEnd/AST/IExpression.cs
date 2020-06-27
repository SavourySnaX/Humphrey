using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public interface IExpression : IAssignable
    {
        CompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder);
    }
}

