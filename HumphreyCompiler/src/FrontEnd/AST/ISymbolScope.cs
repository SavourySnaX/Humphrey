using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public interface ISymbolScope : IAst
    {
        CommonSymbolTable SymbolTable { get; }
    }
}