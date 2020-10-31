using Humphrey.Backend;

namespace Humphrey.FrontEnd
{
    public interface IIdentifier : IAst
    {
        string Name { get; }
    }
}

