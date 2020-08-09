using System.Text;
using Humphrey.Backend;
namespace Humphrey.FrontEnd
{
    public class AstEnumElement : IExpression
    {
        AstIdentifier[] identifiers;
        IAssignable initialiser;
        public AstEnumElement(AstIdentifier[] identifierList, IAssignable init)
        {
            identifiers = identifierList;
            initialiser = init;
        }

        public int NumElements => identifiers.Length;
        public AstIdentifier[] Identifiers => identifiers;
        public string Dump()
        {
            var s = new StringBuilder();
            for (int a=0;a<identifiers.Length;a++)
            {
                if (a!=0)
                    s.Append(" , ");
                s.Append(identifiers[a].Dump());
            }
            s.Append($" := {initialiser.Dump()}");
            return s.ToString();
        }

        public CompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            var expr = initialiser as IExpression;
            if (expr == null)
                throw new System.Exception($"Cannot assign a code block to an enum value");

            return expr.ProcessConstantExpression(unit);
        }

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            throw new System.NotImplementedException();
        }
        private Result<Tokens> _token;
        public Result<Tokens> Token { get => _token; set => _token = value; }

    }
}



