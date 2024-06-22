using System.Numerics;
using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstFloatNumber : IExpression
    {
        string temp;
        public AstFloatNumber(string value)
        {
            temp = value;
        }
    
        public string Dump()
        {
            return temp;
        }
        public ICompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            return new CompilationConstantFloatKind(this);
        }

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            return new CompilationConstantFloatKind(this);
        }

        public IType ResolveExpressionType(SemanticPass pass)
        {
            return new AstFp32Type();
        }

        public void Semantic(SemanticPass pass)
        {
            // Nothing to do
        }

        private Result<Tokens> _token;

        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}
