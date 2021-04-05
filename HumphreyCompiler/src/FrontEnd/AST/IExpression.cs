using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public interface IExpression : IAssignable
    {
        void Semantic(SemanticPass pass);
        IType ResolveExpressionType(SemanticPass pass);
        ICompilationConstantValue ProcessConstantExpression(CompilationUnit unit);
        ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder);
    }
}

