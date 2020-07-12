using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public class AstIdentifier : IExpression,IType
    {
        string temp;
        public AstIdentifier(string value)
        {
            temp = value;
        }
    
        public CompilationType CreateOrFetchType(CompilationUnit unit)
        {
            throw new System.NotImplementedException($"Unimplemented Type create/fetch");
        }
    
        public bool Compile(CompilationUnit unit)
        {
            return false;
        }
        public bool IsFunctionType => false;
    
        public string Dump()
        {
            return temp;
        }
        public CompilationConstantValue ProcessConstantExpression(CompilationUnit unit)
        {
            throw new System.NotImplementedException($"Todo implement constant expression processing for constant values");
        }

        public ICompilationValue ProcessExpression(CompilationUnit unit, CompilationBuilder builder)
        {
            return unit.FetchValue(temp, builder);
        }
    }
}