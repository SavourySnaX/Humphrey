namespace Humphrey.FrontEnd
{
    public interface IOperator : IAst
    {
        enum OperatorKind
        {
            ExpressionExpression,
            ExpressionType,
            ExpressionIdentifier,
        }

        int Precedance { get; }
        OperatorKind RhsKind { get; }
    }
}

