using Humphrey.FrontEnd;

public interface ICompilationValue
{
    Result<Tokens> FrontendLocation { get; }
}