

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace Humphrey.Backend.Tests
{
    public unsafe partial class JitTests
    {
        struct ReturnStructLongInt
        {
            public long a;
            public int b;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static ReturnStructLongInt TestABIReturnStructLongInt()
        {
            return new ReturnStructLongInt { a = 1, b = 2 };
        }

        [Theory]
        [InlineData(@"ReturnStruct:{a:[64]bit b:[32]bit} [C_CALLING_CONVENTION]TestCFunc:()(out:ReturnStruct) Main:()(out:[32]bit)={out=TestCFunc().b;}", "Main", 2)]
        [InlineData(@"ReturnStruct:{a:[64]bit b:[32]bit} [C_CALLING_CONVENTION]TestCFunc:()(out:ReturnStruct) Main:()(out:[32]bit)={out=TestCFunc().a as [32]bit;}", "Main", 1)]
        public void CABI_CheckReturnStructLongInt(string input, string entryPointName, uint expected)
        {
            delegate* unmanaged[Cdecl]<ReturnStructLongInt> TestDelegate = &TestABIReturnStructLongInt;
            var globals = new (string name, nint addr)[] { ("TestCFunc", (nint)TestDelegate) };
            Assert.True(InputVoidExpects32BitValue(CompileForTest(input, entryPointName, globals), expected), $"Test {entryPointName},{expected}");
        }


        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static byte TestABIU8Return(byte v)
        {
            return v;
        }

        [Theory]
        [InlineData(@"[C_CALLING_CONVENTION]TestCFunc:(a:[8]bit)(out:[8]bit) Main:()(out:[32]bit)={out=TestCFunc(0x34);}", "Main", 0x34)]
        [InlineData(@"[C_CALLING_CONVENTION]TestCFunc:(a:[8]bit)(out:[8]bit) Main:()(out:[32]bit)={out=TestCFunc(0x80);}", "Main", 0x80)]
        public void CABI_CheckUInt8Return(string input, string entryPointName, uint expected)
        {
            delegate* unmanaged[Cdecl]<byte, byte> TestDelegate = &TestABIU8Return;
            var globals = new (string name, nint addr)[] { ("TestCFunc", (nint)TestDelegate) };
            Assert.True(InputVoidExpects32BitValue(CompileForTest(input, entryPointName, globals), expected), $"Test {entryPointName},{expected}");
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static sbyte TestABIS8Return(sbyte v)
        {
            return v;
        }

        [Theory]
        [InlineData(@"[C_CALLING_CONVENTION]TestCFunc:(a:[-16]bit)(out:[-16]bit) Main:()(out:[32]bit)={out=TestCFunc(0x12);}", "Main", 0x00000012)]
        [InlineData(@"[C_CALLING_CONVENTION]TestCFunc:(a:[-16]bit)(out:[-16]bit) Main:()(out:[32]bit)={out=TestCFunc(0x80);}", "Main", 0xFFFFFF80)]
        public void CABI_CheckSInt8Return(string input, string entryPointName, uint expected)
        {
            delegate* unmanaged[Cdecl]<sbyte, sbyte> TestDelegate = &TestABIS8Return;
            var globals = new (string name, nint addr)[] { ("TestCFunc", (nint)TestDelegate) };
            Assert.True(InputVoidExpects32BitValue(CompileForTest(input, entryPointName, globals), expected), $"Test {entryPointName},{expected}");
        }



        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static UInt16 TestABIU16Return(UInt16 v)
        {
            return v;
        }

        [Theory]
        [InlineData(@"[C_CALLING_CONVENTION]TestCFunc:(a:[16]bit)(out:[16]bit) Main:()(out:[32]bit)={out=TestCFunc(0x1234);}", "Main", 0x1234)]
        [InlineData(@"[C_CALLING_CONVENTION]TestCFunc:(a:[16]bit)(out:[16]bit) Main:()(out:[32]bit)={out=TestCFunc(0x8000);}", "Main", 0x8000)]
        public void CABI_CheckUInt16Return(string input, string entryPointName, uint expected)
        {
            delegate* unmanaged[Cdecl]<UInt16, UInt16> TestDelegate = &TestABIU16Return;
            var globals = new (string name, nint addr)[] { ("TestCFunc", (nint)TestDelegate) };
            Assert.True(InputVoidExpects32BitValue(CompileForTest(input, entryPointName, globals), expected), $"Test {entryPointName},{expected}");
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static Int16 TestABIS16Return(Int16 v)
        {
            return v;
        }

        [Theory]
        [InlineData(@"[C_CALLING_CONVENTION]TestCFunc:(a:[-16]bit)(out:[-16]bit) Main:()(out:[32]bit)={out=TestCFunc(0x1234);}", "Main", 0x00001234)]
        [InlineData(@"[C_CALLING_CONVENTION]TestCFunc:(a:[-16]bit)(out:[-16]bit) Main:()(out:[32]bit)={out=TestCFunc(0x8000);}", "Main", 0xFFFF8000)]
        public void CABI_CheckSInt16Return(string input, string entryPointName, uint expected)
        {
            delegate* unmanaged[Cdecl]<Int16, Int16> TestDelegate = &TestABIS16Return;
            var globals = new (string name, nint addr)[] { ("TestCFunc", (nint)TestDelegate) };
            Assert.True(InputVoidExpects32BitValue(CompileForTest(input, entryPointName, globals), expected), $"Test {entryPointName},{expected}");
        }

        struct Float2
        {
            public float a;
            public float b;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static float TestABICStruct(Float2 v)
        {
            return v.a + v.b;
        }

        [Theory]
        [InlineData(@"Float2:{a:fp32 b:fp32} [C_CALLING_CONVENTION]TestABIStruct:(a:Float2)(out:fp32) Main:()(out:fp32)={b:Float2=0; b.a=1.0; b.b=2.0; out=TestABIStruct(b);}", "Main", 3.0f)]
        public void CABI_CheckFloatStruct(string input, string entryPointName, float expected)
        {
            delegate* unmanaged[Cdecl]<Float2, float> TestABICStructFP = &TestABICStruct;
            var globals = new (string name, nint addr)[] { ("TestABIStruct", (nint)TestABICStructFP) };
            Assert.True(InputVoidExpectsFloatValue(CompileForTest(input, entryPointName, globals), expected), $"Test {entryPointName},{expected}");
        }

        // ========================================================================
        // P0: fp32 / float register assignment
        // ========================================================================

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static float TestABIFloatReturn(float v)
        {
            return v;
        }

        [Theory]
        [InlineData(@"[C_CALLING_CONVENTION]TestCFunc:(a:fp32)(out:fp32) Main:()(out:fp32)={out=TestCFunc(3.14159265);}", "Main", 3.14159274f)]
        [InlineData(@"[C_CALLING_CONVENTION]TestCFunc:(a:fp32)(out:fp32) Main:()(out:fp32)={out=TestCFunc(2.71828182);}", "Main", 2.71828175f)]
        public void CABI_CheckFloatArgReturn(string input, string entryPointName, float expected)
        {
            delegate* unmanaged[Cdecl]<float, float> TestDelegate = &TestABIFloatReturn;
            var globals = new (string name, nint addr)[] { ("TestCFunc", (nint)TestDelegate) };
            Assert.True(InputVoidExpectsFloatValue(CompileForTest(input, entryPointName, globals), expected), $"Test {entryPointName},{expected}");
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static float TestABIFloatSum(float a, float b)
        {
            return a + b;
        }

        [Theory]
        [InlineData(@"[C_CALLING_CONVENTION]TestCFunc:(a:fp32,b:fp32)(out:fp32) Main:()(out:fp32)={out=TestCFunc(1.5,2.5);}", "Main", 4.0f)]
        [InlineData(@"[C_CALLING_CONVENTION]TestCFunc:(a:fp32,b:fp32)(out:fp32) Main:()(out:fp32)={out=TestCFunc(-1.0,1.0);}", "Main", 0.0f)]
        public void CABI_CheckMultipleFloatArgs(string input, string entryPointName, float expected)
        {
            delegate* unmanaged[Cdecl]<float, float, float> TestDelegate = &TestABIFloatSum;
            var globals = new (string name, nint addr)[] { ("TestCFunc", (nint)TestDelegate) };
            Assert.True(InputVoidExpectsFloatValue(CompileForTest(input, entryPointName, globals), expected), $"Test {entryPointName},{expected}");
        }

        // ========================================================================
        // P0: Large struct return (>12 bytes) — Indirect (hidden pointer)
        // ========================================================================

        struct LargeReturnStruct
        {
            public long a;
            public long b;
            public long c;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static LargeReturnStruct TestABILargeReturn()
        {
            return new LargeReturnStruct { a = 0xDEADBEEF, b = 0xCAFEBABE, c = 0x12345678 };
        }

        [Theory]
        [InlineData(@"LargeStruct:{a:[64]bit b:[64]bit c:[64]bit} [C_CALLING_CONVENTION]TestCFunc:()(out:LargeStruct) Main:()(out:[64]bit)={out=TestCFunc().a;}", "Main", 0xDEADBEEF)]
        [InlineData(@"LargeStruct:{a:[64]bit b:[64]bit c:[64]bit} [C_CALLING_CONVENTION]TestCFunc:()(out:LargeStruct) Main:()(out:[64]bit)={out=TestCFunc().b;}", "Main", 0xCAFEBABE)]
        [InlineData(@"LargeStruct:{a:[64]bit b:[64]bit c:[64]bit} [C_CALLING_CONVENTION]TestCFunc:()(out:LargeStruct) Main:()(out:[64]bit)={out=TestCFunc().c;}", "Main", 0x12345678)]
        public void CABI_CheckLargeStructReturn(string input, string entryPointName, ulong expected)
        {
            delegate* unmanaged[Cdecl]<LargeReturnStruct> TestDelegate = &TestABILargeReturn;
            var globals = new (string name, nint addr)[] { ("TestCFunc", (nint)TestDelegate) };
            Assert.True(InputVoidExpects64BitValue(CompileForTest(input, entryPointName, globals), expected), $"Test {entryPointName},{expected}");
        }

        // ========================================================================
        // P1: Large struct argument (>12 bytes) — Indirect encoding
        // ========================================================================

        struct LargeArgStruct
        {
            public long a;
            public long b;
            public long c;
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static long TestABILargeArg(LargeArgStruct v)
        {
            return v.a + v.b + v.c;
        }

        [Theory]
        [InlineData(@"LargeArg:{a:[64]bit b:[64]bit c:[64]bit} [C_CALLING_CONVENTION]TestCFunc:(a:LargeArg)(out:[64]bit) Main:()(out:[64]bit)={x:LargeArg=0; x.a=100; x.b=200; x.c=300; out=TestCFunc(x);}", "Main", 600)]
        [InlineData(@"LargeArg:{a:[64]bit b:[64]bit c:[64]bit} [C_CALLING_CONVENTION]TestCFunc:(a:LargeArg)(out:[64]bit) Main:()(out:[64]bit)={x:LargeArg=0; x.a=-100; x.b=50; x.c=0; out=TestCFunc(x);}", "Main", -50)]
        public void CABI_CheckLargeStructArg(string input, string entryPointName, long expected)
        {
            delegate* unmanaged[Cdecl]<LargeArgStruct, long> TestDelegate = &TestABILargeArg;
            var globals = new (string name, nint addr)[] { ("TestCFunc", (nint)TestDelegate) };
            Assert.True(InputVoidExpects64BitValue(CompileForTest(input, entryPointName, globals), (ulong)expected), $"Test {entryPointName},{expected}");
        }

        // ========================================================================
        // P1: Multiple integer args exhausting register budget (>8 int args)
        // ========================================================================

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static ulong TestABIMultiIntArg(ulong a, ulong b, ulong c, ulong d, ulong e, ulong f, ulong g, ulong h, ulong i)
        {
            return a + b + c + d + e + f + g + h + i;
        }

        [Theory]
        [InlineData(@"[C_CALLING_CONVENTION]TestCFunc:(a:[64]bit,b:[64]bit,c:[64]bit,d:[64]bit,e:[64]bit,f:[64]bit,g:[64]bit,h:[64]bit,i:[64]bit)(out:[64]bit) Main:()(out:[64]bit)={out=TestCFunc(1,2,3,4,5,6,7,8,9);}", "Main", 45UL)]
        [InlineData(@"[C_CALLING_CONVENTION]TestCFunc:(a:[64]bit,b:[64]bit,c:[64]bit,d:[64]bit,e:[64]bit,f:[64]bit,g:[64]bit,h:[64]bit,i:[64]bit)(out:[64]bit) Main:()(out:[64]bit)={out=TestCFunc(100,200,300,400,500,600,700,800,900);}", "Main", 4500UL)]
        public void CABI_CheckExhaustIntegerRegisters(string input, string entryPointName, ulong expected)
        {
            delegate* unmanaged[Cdecl]<ulong, ulong, ulong, ulong, ulong, ulong, ulong, ulong, ulong, ulong> TestDelegate = &TestABIMultiIntArg;
            var globals = new (string name, nint addr)[] { ("TestCFunc", (nint)TestDelegate) };
            Assert.True(InputVoidExpects64BitValue(CompileForTest(input, entryPointName, globals), expected), $"Test {entryPointName},{expected}");
        }

        // ========================================================================
        // P2: Mixed int+FP argument ordering
        // ========================================================================

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static double TestABIMixedArgs(ulong a, double b, ulong c, double d)
        {
            return (double)(a + c) + b + d;
        }

        [Theory]
        [InlineData(@"[C_CALLING_CONVENTION]TestCFunc:(a:[64]bit,b:fp64,c:[64]bit,d:fp64)(out:fp64) Main:()(out:fp64)={out=TestCFunc(0,0.0,0,0.0);}", "Main", 0.0)]
        public void CABI_CheckMixedIntAndFpArgs(string input, string entryPointName, double expected)
        {
            delegate* unmanaged[Cdecl]<ulong, double, ulong, double, double> TestDelegate = &TestABIMixedArgs;
            var globals = new (string name, nint addr)[] { ("TestCFunc", (nint)TestDelegate) };
            Assert.True(InputVoidExpectsDoubleValue(CompileForTest(input, entryPointName, globals), expected), $"Test {entryPointName},{expected}");
        }

        // ========================================================================
        // P3: CoerceType consistency
        // ========================================================================

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        static Float2 TestABICastConsistency(Float2 v)
        {
            return new Float2 { a = v.a * 2.0f, b = v.b * 3.0f };
        }

        [Theory]
        [InlineData(@"Float2:{a:fp32 b:fp32} [C_CALLING_CONVENTION]TestCFunc:(a:Float2)(out:Float2) Main:()(out:fp32)={b:Float2=0; b.a=5.0; b.b=10.0; out=TestCFunc(b).b;}", "Main", 30.0f)]
        [InlineData(@"Float2:{a:fp32 b:fp32} [C_CALLING_CONVENTION]TestCFunc:(a:Float2)(out:Float2) Main:()(out:fp32)={b:Float2=0; b.a=7.0; b.b=11.0; out=TestCFunc(b).a;}", "Main", 14.0f)]
        public void CABI_CheckCastConsistency(string input, string entryPointName, float expected)
        {
            delegate* unmanaged[Cdecl]<Float2, Float2> TestDelegate = &TestABICastConsistency;
            var globals = new (string name, nint addr)[] { ("TestCFunc", (nint)TestDelegate) };
            Assert.True(InputVoidExpectsFloatValue(CompileForTest(input, entryPointName, globals), expected), $"Test {entryPointName},{expected}");
        }

    }
}