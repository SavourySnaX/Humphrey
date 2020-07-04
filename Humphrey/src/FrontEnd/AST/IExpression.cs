using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public interface IExpression : IAssignable
    {
        CompilationConstantValue ProcessConstantExpression(CompilationUnit unit);
        CompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder);
    }
}

