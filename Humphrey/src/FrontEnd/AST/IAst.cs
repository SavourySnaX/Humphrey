using Humphrey.Backend;

namespace Humphrey.FrontEnd
{

    public interface IAst
    {
        Result<Tokens> Token { get; set; }
        string Dump();
    }
}