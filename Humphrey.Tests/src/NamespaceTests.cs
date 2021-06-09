using Xunit;

using Humphrey.FrontEnd;
using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.IO;
using LibGit2Sharp;

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

InIsOutUInt8:(in:UInt8)(out:UInt8)=
{
    out=in;
}

AllOnesUInt64:()(out:UInt64)=
{
    out=-1;
}

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

        private IPackageManager GetPackageManagerForGitTest([CallerFilePath] string currentFolder="")
        {
            return new GitPackageManager(Repository.Discover(Directory.GetCurrentDirectory()), "7d22c3da73526b0f28cbddfc8460dc77559cc9d7");
        }

        private IPackageManager GetPackageManagerForDefault([CallerFilePath] string currentFolder="")
        {
            var t = new IPackageManager[1];
            t[0] = GetPackageManagerForFileSystemTests();
            return new DefaultPackageManager(t);
        }

        [Theory]
        // direct reference
        [InlineData("Main:()(returnValue:System::Types::UInt8)={returnValue=0x93;} ", 0x93)]
        [InlineData("Main:()(returnValue:System::Types::UInt8)={returnValue=(System::Types::AllOnesUInt64() as System::Types::UInt8);} ", 0xFF)]
        [InlineData("Main:()(returnValue:System::Types::UInt8)={returnValue=System::Types::InIsOutUInt8(12);} ", 12)]
        [InlineData("Main:()(returnValue:System::Types::UInt8)={returnValue=System::Types::InIsOutUInt8(12 as System::Types::UInt8);} ", 12)]
        [InlineData("Main:()(returnValue:System::Types::UInt8)={returnValue=System::Types::MemorySizeOf(returnValue) as System::Types::UInt8;} ", 0x1)]
        // using
        [InlineData("using System::Types Main:()(returnValue:UInt8)={returnValue=0x93;} ", 0x93)]
        [InlineData("using System::Types Main:()(returnValue:UInt8)={returnValue=(AllOnesUInt64() as UInt8);} ", 0xFF)]
        [InlineData("using System::Types Main:()(returnValue:UInt8)={returnValue=InIsOutUInt8(12);} ", 12)]
        [InlineData("using System::Types Main:()(returnValue:UInt8)={returnValue=InIsOutUInt8(12 as UInt8);} ", 12)]
        [InlineData("using System::Types Main:()(returnValue:UInt8)={returnValue=MemorySizeOf(returnValue) as UInt8;} ", 0x1)]
        // using and direct reference
        [InlineData("using System::Types Main:()(returnValue:System::Types::UInt8)={returnValue=0x93;} ", 0x93)]
        [InlineData("using System::Types Main:()(returnValue:System::Types::UInt8)={returnValue=(AllOnesUInt64() as UInt8);} ", 0xFF)]
        [InlineData("using System::Types Main:()(returnValue:System::Types::UInt8)={returnValue=InIsOutUInt8(12);} ", 12)]
        [InlineData("using System::Types Main:()(returnValue:System::Types::UInt8)={returnValue=InIsOutUInt8(12 as UInt8);} ", 12)]
        [InlineData("using System::Types Main:()(returnValue:System::Types::UInt8)={returnValue=MemorySizeOf(returnValue) as UInt8;} ", 0x1)]
        public void CheckNamespaceTest(string input, byte result)
        {
            BuildForTest(input, result, GetPackageManagerForTests());
            BuildForTest(input, result, GetPackageManagerForFileSystemTests());
            BuildForTest(input, result, GetPackageManagerForDefault());
            BuildForTest(input, result, GetPackageManagerForGitTest());
        }


        [Theory]
        [InlineData("using System::Types using Test1 Main:()(returnValue:UInt8)={returnValue=FunctionInTest1();} ", 0x42)]
        public void CheckMultiFile(string input, byte result)
        {
            var p = new TestPackageManager();
            p.AddAnEntry("System/Types.humphrey", SystemTypes);
            p.AddAnEntry("Test1.humphrey", "using System::Types FunctionInTest1:()(out:UInt8)={ out = 0x42; }");
            BuildForTest(input, result, p);
        }
        
        [Theory]
        [InlineData("using Test1 Main:()(returnValue:[8]bit)={ returnValue=0x42;} ", 0x42)]
        public void CheckDeepFile(string input, byte result)
        {
            var p = new TestPackageManager();
            p.AddAnEntry("System/Types.humphrey", SystemTypes);
            p.AddAnEntry("Test1.humphrey", "using Test2 StructInTest1:{ptr:*StructInTest2}");
            p.AddAnEntry("Test2.humphrey", "StructInTest2:{val:[64]bit}");
            BuildForTest(input, result, p);
        }
        
        [Theory]
        [InlineData("using System::Types using Test1 Main:()(returnValue:UInt8)={ returnValue=0x42;} ", 0x42)]
        public void CheckDuplicates(string input, byte result)
        {
            var p = new TestPackageManager();
            p.AddAnEntry("System/Types.humphrey", SystemTypes);
            p.AddAnEntry("Test1.humphrey", "using Test2 StructInTest1:{ptr:*StructInTest2}");
            p.AddAnEntry("Test2.humphrey", "using System::Types StructInTest2:{val:UInt64}");
            BuildForTest(input, result, p);
        }

        private void BuildForTest(string input, byte expected, IPackageManager manager)
        {
            var messages = new CompilerMessages(true, true, false);
            var tokenise = new HumphreyTokeniser(messages);
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens, messages);
            var parsed = parser.File();
            var semantic = new SemanticPass(manager, messages);
            semantic.RunPass(parsed);
            var compiler = new HumphreyCompiler(messages);
            CompilationUnit unit=null;
            if (!messages.HasErrors)
            {
                unit = compiler.Compile(semantic, "test", "x86_64", false, true);
            }

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


