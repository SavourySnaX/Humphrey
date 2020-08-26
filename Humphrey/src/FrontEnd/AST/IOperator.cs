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

        int BinaryPrecedance { get; }
        int UnaryPrecedance { get; }
        OperatorKind RhsKind { get; }
    }
}

