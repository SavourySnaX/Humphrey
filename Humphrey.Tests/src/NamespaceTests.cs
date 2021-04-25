using Xunit;

using Humphrey.FrontEnd;
using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.IO;

namespace Humphrey.Backend.Tests
{


    public class TestPackageManager : IPackageManager
    {
        public class TestPackageEntry : IPackageEntry, IPackageLevel
        {
            private IPackageLevel _parent;
            private string _contents;

            public TestPackageEntry(IPackageLevel parent, string contents)
            {
                _parent = parent;
                _contents = contents;
            }

            public IPackageLevel FetchEntry(string name)
            {
                return null;
            }

            public string Contents => _contents;
        }
        public class TestPackageLevel : IPackageLevel
        {
            private IPackageLevel _parent;
            private Dictionary<string, IPackageLevel> _contents;

            public TestPackageLevel(IPackageLevel parent)
            {
                _parent = parent;
                _contents = new Dictionary<string, IPackageLevel>();
            }

            public IPackageLevel FetchEntry(string name)
            {
                if (_contents.TryGetValue(name, out var result))
                {
                    return result;
                }
                return null;
            }

            public void AddEntry(string name, IPackageLevel entry)
            {
                _contents.Add(name, entry);
            }
        }

        TestPackageLevel root;

        public IPackageLevel FetchRoot => root;

        public TestPackageManager()
        {
            root = new TestPackageLevel(null);
        }

        public void AddAnEntry(string path, string contents)
        {
            var seperated = path.Split('/');
            var current = root;
            var filenameEnd = ".humphrey";
            foreach (var s in seperated)
            {
                var next = current.FetchEntry(s);
                if (next == null)
                {
                    if (s.EndsWith(filenameEnd))
                    {
                        var entry = new TestPackageEntry(current, contents);
                        current.AddEntry(s.Substring(0, s.LastIndexOf(filenameEnd)), entry);
                        // This would be the last iteration
                    }
                    else
                    {
                        var entry = new TestPackageLevel(current);
                        current.AddEntry(s, entry);
                        current = entry;
                    }
                }
            }
        }
    }

    public unsafe class NamespaceTests
    {
        const string SystemTypes = @"
Int8  : [-8]bit
Int16 : [-16]bit
Int32 : [-32]bit
Int64 : [-64]bit

UInt8  : [8]bit
UInt16 : [16]bit
UInt32 : [32]bit
UInt64 : [64]bit

MemorySizeOf:(type:_)(size:UInt64)=
{
    ptr:=&type;
    size = ((&ptr[1]) as UInt64)-((&ptr[0]) as UInt64);
}
";

        public TestPackageManager GetPackageManagerForTests()
        {
            var p = new TestPackageManager();
            p.AddAnEntry("System/Types.humphrey", SystemTypes);
            return p;
        }
        private IPackageManager GetPackageManagerForFileSystemTests([CallerFilePath] string currentFolder="")
        {
            return new FileSystemPackageManager(Path.Combine(Path.GetDirectoryName(currentFolder),"FileSystemNamespaceTestRoot"));
        }

        private IPackageManager GetPackageManagerForDefault([CallerFilePath] string currentFolder="")
        {
            var t = new IPackageManager[1];
            t[0] = GetPackageManagerForFileSystemTests();
            return new DefaultPackageManager(t);
        }

        [Theory]
        [InlineData("Main:()(returnValue:System::Types::UInt8)={returnValue=0x93;} ", 0x93)]
        //[InlineData("using System Main:()(returnValue:Types.UInt8)={returnValue=0;} ")]
        //[InlineData("using System/Types Main:()(returnValue:UInt8)={returnValue=0;} ")]
        public void CheckNamespaceTest(string input, byte result)
        {
            BuildForTest(input, result, GetPackageManagerForTests());
            BuildForTest(input, result, GetPackageManagerForFileSystemTests());
            BuildForTest(input, result, GetPackageManagerForDefault());
        }
        
        private void BuildForTest(string input, byte expected, IPackageManager manager)
        {
            var messages = new CompilerMessages(true, true, false);
            var tokenise = new HumphreyTokeniser(messages);
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens, messages);
            var parsed = parser.File();
            var semantic = new SemanticPass(manager.FetchRoot, messages);
            semantic.RunPass(parsed);
            var compiler = new HumphreyCompiler(messages);
            var unit = compiler.Compile(semantic.RootSymbolTable, manager, parsed, "test", "x86_64", false, true);

            if (messages.HasErrors)
            {
                throw new Exception($"{messages.Dump()}");
            }

            var ptr = unit.JitMethod("Main");

            Assert.True(InputVoidExpects8BitValue(ptr, expected));
        }

        delegate void InputVoidOutput8Bit(byte* returnVal);

        public static bool InputVoidExpects8BitValue(IntPtr ee, byte expected)
        {
            var func = Marshal.GetDelegateForFunctionPointer<InputVoidOutput8Bit>(ee);
            byte returnValue;
            func(&returnValue);
            return returnValue == expected;
        }

    }
}


