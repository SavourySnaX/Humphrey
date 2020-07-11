using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public interface IExpression : IAssignable
    {
        CompilationConstantValue ProcessConstantExpression(CompilationUnit unit);
        ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder);
    }
}

