using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public interface IStorable : IExpression
    {
        void ProcessExpressionForStore(CompilationUnit unit, CompilationBuilder builder, IExpression value);
    }
}