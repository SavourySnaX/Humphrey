namespace Humphrey.FrontEnd
{
    public interface IOperator : IAst
    {
        enum OperatorKind
        {
            ExpressionExpression,
            ExpressionType,
            ExpressionIdentifier,
            ExpressionExpressionList,
            ExpressionExpressionContinuation,
        }

        int Precedance { get; }
        OperatorKind RhsKind { get; }
    }
}

