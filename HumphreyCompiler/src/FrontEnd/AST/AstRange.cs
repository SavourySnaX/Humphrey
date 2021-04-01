using System.Text;
using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public class AstRange : IAst
    {
        IExpression inclusiveStart;
        IExpression exclusiveStop;
        public AstRange(IExpression inclusiveBegin, IExpression exclusiveEnd)
        {
            inclusiveStart = inclusiveBegin;
            exclusiveStop = exclusiveEnd;
        }

        public string Dump()
        {
            return $"{inclusiveStart.Dump()} .. {exclusiveStop.Dump()}";
        }

        public IExpression InclusiveStart => inclusiveStart;
        public IExpression ExclusiveEnd => exclusiveStop;
        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}




