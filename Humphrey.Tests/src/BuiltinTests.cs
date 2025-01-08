using Humphrey.FrontEnd;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace Humphrey.Backend.Tests
{
    public unsafe partial class JitTests
    {
        // Note - need to ensure things are aligned to avoid calls to __atomic_load being emitted, but that is a bigger challenge

        const string Ordering = "Intrinsic_MemoryOrder:[32]bit{Relaxed:=0 Consume:=1 Acquire:=2 Release:=3 AcquireRelease:=4 SequentiallyConsistent:=5}";

        [Theory]
        [InlineData($"{Ordering} [BUILT_IN]Intrinsic_AtomicLoadExplicit:(ptr:*[64]bit,order:Intrinsic_MemoryOrder)(out:[64]bit) Main:()(out:[64]bit)={{val:[64]bit=999;    out=Intrinsic_AtomicLoadExplicit(&val, Intrinsic_MemoryOrder.Acquire);}}", "Main", 999)]
        public void BuiltIn_LoadAtomic(string input, string entryPointName, UInt64 expected)
        {
            Assert.True(InputVoidExpects64BitValue(CompileForTest(input, entryPointName), expected), $"Test {entryPointName},{expected}");
        }
        
        [Theory]
        [InlineData($"{Ordering} [BUILT_IN]Intrinsic_AtomicStoreExplicit:(ptr:*[64]bit,val:[64]bit,order:Intrinsic_MemoryOrder)() Main:()(out:[64]bit)={{val:[64]bit=_; Intrinsic_AtomicStoreExplicit(&val,999,Intrinsic_MemoryOrder.Release); out=val;}}", "Main", 999)]
        public void BuiltIn_StoreAtomic(string input, string entryPointName, UInt64 expected)
        {
            Assert.True(InputVoidExpects64BitValue(CompileForTest(input, entryPointName), expected), $"Test {entryPointName},{expected}");
        }

        const string LocalVec2F = "MyVec2:{a:fp32 b:fp32}";
        const string anonVec2F = "{x:fp32 y:fp32}";
        const string MakeVec2F = "MakeVec:()(result:MyVec2)={result=0;}";

        [Theory]
        [InlineData($"{LocalVec2F} {MakeVec2F} [BUILT_IN]Intrinsic_Vec2FAdd:(a:{anonVec2F},b:{anonVec2F})(result:{anonVec2F}) Main:(i1:fp32,i2:fp32)(out:fp32)={{s:=MakeVec().result; t:=MakeVec().result; s.a=i1; t.a=i2; s=Intrinsic_Vec2FAdd(s,t); out=s.a;}}", "Main", 8, 8, 16)]
        public void BuiltIn_Vec2Add(string input, string entryPointName, float a, float b, float expected)
        {
            Assert.True(InputFloatFloatExpectsFloatValue(CompileForTest(input, entryPointName), a, b, expected), $"Test {entryPointName},{expected}");
        }

        [Theory]
        [InlineData($"{LocalVec2F} {MakeVec2F} [BUILT_IN]Intrinsic_Vec2FSub:(a:{anonVec2F},b:{anonVec2F})(result:{anonVec2F}) Main:(i1:fp32,i2:fp32)(out:fp32)={{s:=MakeVec().result; t:=MakeVec().result; s.a=i1; t.a=i2; s=Intrinsic_Vec2FSub(s,t); out=s.a;}}", "Main", 8, 2, 6)]
        public void BuiltIn_Vec2Sub(string input, string entryPointName, float a, float b, float expected)
        {
            Assert.True(InputFloatFloatExpectsFloatValue(CompileForTest(input, entryPointName), a, b, expected), $"Test {entryPointName},{expected}");
        }

        [Theory]
        [InlineData($"{LocalVec2F} {MakeVec2F} [BUILT_IN]Intrinsic_Vec2FMul:(a:{anonVec2F},b:{anonVec2F})(result:{anonVec2F}) Main:(i1:fp32,i2:fp32)(out:fp32)={{s:=MakeVec().result; t:=MakeVec().result; s.a=i1; t.a=i2; s=Intrinsic_Vec2FMul(s,t); out=s.a;}}", "Main", 8, 8, 64)]
        public void BuiltIn_Vec2Mul(string input, string entryPointName, float a, float b, float expected)
        {
            Assert.True(InputFloatFloatExpectsFloatValue(CompileForTest(input, entryPointName), a, b, expected), $"Test {entryPointName},{expected}");
        }

        [Theory]
        [InlineData($"{LocalVec2F} {MakeVec2F} [BUILT_IN]Intrinsic_Vec2FDiv:(a:{anonVec2F},b:{anonVec2F})(result:{anonVec2F}) Main:(i1:fp32,i2:fp32)(out:fp32)={{s:=MakeVec().result; t:=MakeVec().result; s.a=i1; t.a=i2; s=Intrinsic_Vec2FDiv(s,t); out=s.a;}}", "Main", 8, 2, 4)]
        public void BuiltIn_Vec2Div(string input, string entryPointName, float a, float b, float expected)
        {
            Assert.True(InputFloatFloatExpectsFloatValue(CompileForTest(input, entryPointName), a, b, expected), $"Test {entryPointName},{expected}");
        }
        
        [Theory]
        [InlineData($"{LocalVec2F} {MakeVec2F} [BUILT_IN]Intrinsic_Vec2FDot:(a:{anonVec2F},b:{anonVec2F})(result:fp32) Main:(i1:fp32,i2:fp32)(out:fp32)={{s:=MakeVec().result; t:=MakeVec().result; s.a=i1; t.a=i2; out=Intrinsic_Vec2FDot(s,t);}}", "Main", 1, 1, 1)]
        public void BuiltIn_Vec2Dot(string input, string entryPointName, float a, float b, float expected)
        {
            Assert.True(InputFloatFloatExpectsFloatValue(CompileForTest(input, entryPointName), a, b, expected), $"Test {entryPointName},{expected}");
        }

        [Theory]
        [InlineData($"{LocalVec2F} {MakeVec2F} [BUILT_IN]Intrinsic_Vec2FFloor:(a:{anonVec2F})(result:{anonVec2F}) Main:(i1:fp32,i2:fp32)(out:fp32)={{s:=MakeVec().result; s.a=i1; s.b=i2; s=Intrinsic_Vec2FFloor(s); out=s.a;}}", "Main", 1.9, 0, 1)]
        public void BuiltIn_Vec2Floor(string input, string entryPointName, float a, float b, float expected)
        {
            Assert.True(InputFloatFloatExpectsFloatValue(CompileForTest(input, entryPointName), a, b, expected), $"Test {entryPointName},{expected}");
        }
    }
}
