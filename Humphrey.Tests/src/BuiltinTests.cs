using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace Humphrey.Backend.Tests
{
    public unsafe partial class JitTests
    {
        const string Ordering = "Intrinsic_MemoryOrder:[32]bit{Relaxed:=0 Consume:=1 Acquire:=2 Release:=3 AcquireRelease:=4 SequentiallyConsistent:=5}";

        [Theory]
        [InlineData($"{Ordering} [BUILT_IN]Intrinsic_AtomicLoadExplicit:(ptr:*[64]bit,order:Intrinsic_MemoryOrder)(out:[64]bit) Main:()(out:[64]bit)={{val:[64]bit=999;    out=Intrinsic_AtomicLoadExplicit(&val, Intrinsic_MemoryOrder.Acquire);}}", "Main", 999)]
        public void BuiltIn_LoadAtomic(string input, string entryPointName, UInt64 expected)
        {
            Assert.True(InputVoidExpects64BitValue(CompileForTest(input, entryPointName), expected), $"Test {entryPointName},{expected}");
        }
    }
}
