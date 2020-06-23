namespace Humphrey.FrontEnd
{
    public interface IOperator : IAst
    {
        int Precedance { get; }
    }
}

