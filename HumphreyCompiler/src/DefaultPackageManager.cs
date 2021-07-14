
namespace Humphrey
{
    //Acts as a container to allow multiple package managers
    public class DefaultPackageManager : IPackageManager, IPackageLevel
    {
        IPackageManager[] _managers;
        public DefaultPackageManager(IPackageManager[] managers)
        {
            _managers = managers;
        }

        public IPackageLevel FetchRoot => this;

        public IPackageLevel FetchEntry(string name)
        {
            // Loop through managers requesting the entry until we either find one, or fail
            foreach (var m in _managers)
            {
                var entry = m.FetchRoot.FetchEntry(name);
                if (entry != null)
                {
                    return entry;
                }
            }
            return null;
        }
    }
}