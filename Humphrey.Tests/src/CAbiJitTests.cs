

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

    }
}