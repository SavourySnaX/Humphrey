using Humphrey.Backend;
using LLVMSharp.Interop;

namespace Humphrey.FrontEnd
{
    public class AstUnaryAddressOf : IExpression
    {
        IExpression expr;
        public AstUnaryAddressOf(IExpression expression)
        {
            expr = expression;
        }
    
        public string Dump()
        {
            return $"& {expr.Dump()}";
        }

        public ICompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            throw new System.NotImplementedException("Cant take address of constant");
        }

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            var value = expr.ProcessExpression(unit, builder);
            if (value is CompilationConstantIntegerKind constantValue)
            {
                throw new System.NotImplementedException("Cant take address of constant");
            }
            else
            {
                var compilationValue = value as CompilationValue;
                return compilationValue.Storage;
            }
        }

        public IType ResolveExpressionType(SemanticPass pass)
        {
            var t = expr.ResolveExpressionType(pass);
            if (t==null)
            {
                throw new System.NotImplementedException($"TODO error undefined type?");
            }
            return new AstPointerType(t);
        }

        public void Semantic(SemanticPass pass)
        {
            expr.Semantic(pass);
        }

        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}


