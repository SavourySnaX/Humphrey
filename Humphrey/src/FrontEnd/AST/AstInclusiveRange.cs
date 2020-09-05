using System.Text;
using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public class AstInclusiveRange : IAst, IExpression
    {
        IExpression inclusiveStart;
        IExpression inclusiveStop;
        public AstInclusiveRange(IExpression inclusiveBegin, IExpression inclusiveEnd)
        {
            inclusiveStart = inclusiveBegin;
            inclusiveStop = inclusiveEnd;
        }

        public string Dump()
        {
            if (inclusiveStop==null)
                return $"{inclusiveStart.Dump()} ..";
            else if (InclusiveStart==null)
                return $".. {inclusiveStop.Dump()}";
            return $"{inclusiveStart.Dump()} .. {inclusiveStop.Dump()}";
        }

        public CompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            throw new System.NotImplementedException($"Should never be directly called");
        }

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            throw new System.NotImplementedException($"Should never be directly called");
        }

        public IExpression InclusiveStart => inclusiveStart;
        public IExpression InclusiveEnd => inclusiveStop;
        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}



