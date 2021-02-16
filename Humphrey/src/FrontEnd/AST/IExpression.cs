using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public interface IExpression : IAssignable
    {
        ICompilationConstantValue ProcessConstantExpression(CompilationUnit unit);
        ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder);
    }
}

