namespace Humphrey.Backend
{
    public static class Expression
    {
        public static CompilationValue ResolveExpressionToValue(CompilationUnit unit, ICompilationValue expression, CompilationType type)
        {
            CompilationValue value = expression as CompilationValue;
            if (expression is CompilationConstantValue ccv)
                value = ccv.GetCompilationValue(unit, type);
            return value;
        }
    }
}