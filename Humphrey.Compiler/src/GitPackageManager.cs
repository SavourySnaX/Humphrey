
using System.Collections.Generic;
using LibGit2Sharp;

namespace Humphrey
{
    public class GitPackageManager : IPackageManager
    {
        protected class GitPackageEntry : IPackageEntry, IPackageLevel
        {
            string _contents;

            public GitPackageEntry(Blob gitBlob)
            {
                _contents = gitBlob.GetContentText();
            }

            public string Contents => _contents;
			public string Path => "";

			public IPackageLevel FetchEntry(string name)
            {
                return null;
            }
        }
        protected class GitPackageLevel : IPackageLevel
        {
            private Tree _tree;
            private Dictionary<string, IPackageLevel> _contents;
            public GitPackageLevel(Tree tree)
            {
                _tree = tree;
                _contents = new Dictionary<string, IPackageLevel>();
            }
            public IPackageLevel FetchEntry(string name)
            {
                if (_contents.TryGetValue(name, out var result))
                {
                    return result;
                }
                // perhaps not cached yet so scan tree
                foreach(var e in _tree)
                {
                    var matchName = e.Name;
                    if (e.Name.EndsWith(".humphrey"))
                        matchName = e.Name.Substring(0, e.Name.LastIndexOf(".humphrey"));
                    if (matchName==name)
                    {
                        if (e.TargetType == TreeEntryTargetType.Tree)
                        {
                            var level = new GitPackageLevel(e.Target.Peel<Tree>());
                            _contents.Add(name, level);
                            return level;
                        }
                        else if (e.TargetType == TreeEntryTargetType.Blob)
                        {
                            var entry = new GitPackageEntry(e.Target.Peel<Blob>());
                            _contents.Add(name, entry);
                            return entry;
                        }
                        else
                        {
                            throw new System.NotImplementedException($"Not a valid humprey package");
                        }
                    }
                }
                return null;
            }
        }

        private GitPackageLevel _root;

        public GitPackageManager(string repository, string revision)
        {
            var repo = new Repository(repository);
            repo.RevParse(revision, out var reference, out var obj);
            _root = new GitPackageLevel(obj.Peel<Tree>());
        }

        public IPackageLevel FetchRoot => _root;
    }
}