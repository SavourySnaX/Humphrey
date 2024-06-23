
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Humphrey
{
    public class PackageManager
    {
        public class GitPackageConfig
        {
            public string repo { get; set; }
            public string revision { get; set; }
        }
        public class Config
        {
            public string root { get; set; }
            public GitPackageConfig[] git { get; set; }
        }

        private readonly IPackageManager _manager;

        public PackageManager(): this("humphrey.json"){}
        public PackageManager(string packageJson)
        {
            var config = File.ReadAllText(packageJson);
            var json = JsonSerializer.Deserialize<Config>(config);
            var list = new List<IPackageManager>();
            list.Add(new FileSystemPackageManager(Path.GetDirectoryName(Path.GetFullPath(json.root))));
            foreach (var g in json.git)
            {
                list.Add(new GitPackageManager(g.repo, g.revision));
            }
            _manager = new DefaultPackageManager(list.ToArray());
        }

        public IPackageManager Manager => _manager;
    }
}