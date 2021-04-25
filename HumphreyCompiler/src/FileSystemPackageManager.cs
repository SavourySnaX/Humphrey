
using System.Collections.Generic;
using System.IO;

namespace Humphrey
{
    public class FileSystemPackageManager : IPackageManager
    {
        FileSystemLevel _root;

        protected class FileSystemEntry : IPackageEntry, IPackageLevel
        {
            string _contents;

            public FileSystemEntry(FileInfo file)
            {
                using (var s = file.OpenText())
                {
                    _contents = s.ReadToEnd();
                }
            }

            public string Contents => _contents;

            public IPackageLevel FetchEntry(string name)
            {
                return null;
            }
        }
        protected class FileSystemLevel : IPackageLevel
        {
            Dictionary<string, IPackageLevel> _contents;
            DirectoryInfo _levelInfo;

            public FileSystemLevel(DirectoryInfo dirInfo)
            {
                _levelInfo = dirInfo;
                _contents = new Dictionary<string, IPackageLevel>();
            }

            public IPackageLevel FetchEntry(string name)
            {
                if (_contents.TryGetValue(name, out var value))
                {
                    return value;
                }
                // otherwise we may not have visited before
                foreach (var d in _levelInfo.EnumerateDirectories())
                {
                    if (d.Name == name)
                    {
                        var newDir = new FileSystemLevel(d);
                        _contents.Add(name,newDir);
                        return newDir;
                    }
                }
                // otherwise it might be a file
                foreach (var f in _levelInfo.EnumerateFiles("*.humphrey"))
                {
                    var matchName = f.Name.Substring(0, f.Name.LastIndexOf(".humphrey"));
                    if (matchName==name)
                    {
                        var newEntry = new FileSystemEntry(f);
                        _contents.Add(name, newEntry);
                        return newEntry;
                    }
                }
                return null;
            }
        }

        public FileSystemPackageManager(string root)
        {
            _root = new FileSystemLevel(new DirectoryInfo(root));
        }

        public IPackageLevel FetchRoot => _root;
    }
}