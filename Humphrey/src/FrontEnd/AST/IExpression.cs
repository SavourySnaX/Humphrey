using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public interface IExpression : IAssignable
    {
        CompilationValue ProcessConstantExpression(CompilationUnit unit);
        CompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder);
    }
}

