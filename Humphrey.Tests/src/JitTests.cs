using Xunit;

using Humphrey.FrontEnd;
using System.Runtime.InteropServices;
using System;

namespace Humphrey.Backend.tests
{
    public unsafe class JitTests
    {
        [Theory]
        [InlineData(@" Main : () (returnValue : bit) = { returnValue = 0 } ", "Main", 0)]
        [InlineData(@" Main : () (returnValue : bit) = { returnValue = 1 } ", "Main", 1)]
        [InlineData(@" Main : () (returnValue : bit) = { returnValue = +1 } ", "Main", 1)]
        [InlineData(@" Main : () (returnValue : bit) = { returnValue = 1+0 } ", "Main", 1)]
        [InlineData(@" Main : () (returnValue : bit) = { returnValue = 1-1 } ", "Main", 0)]
        [InlineData(@" Main : () (returnValue : bit) = { returnValue = 1*1 } ", "Main", 1)]
        [InlineData(@" Main : () (returnValue : bit) = { returnValue = 1/1 } ", "Main", 1)]
        [InlineData(@" Main : () (returnValue : bit) = { returnValue = 1%1 } ", "Main", 0)]
        public void CheckVoidExpectsBit(string input, string entryPointName, byte expected)
        {
            Assert.True(InputVoidExpectsBitValue(CompileForTest(input, entryPointName), expected), $"Test {entryPointName},{input}");
        }

        [Theory]
        [InlineData(@" Main : () (returnValue : bit) = { local:bit=0 returnValue=local } ", "Main", 0)]
        [InlineData(@" Main : () (returnValue : bit) = { local:bit=+1 returnValue=local } ", "Main", 1)]
        [InlineData(@" Main : () (returnValue : bit) = { local:bit=1+0 returnValue=local } ", "Main", 1)]
        [InlineData(@" Main : () (returnValue : bit) = { local:bit= 1-1  returnValue=local} ", "Main", 0)]
        [InlineData(@" Main : () (returnValue : bit) = { local:bit=1*1 returnValue=local } ", "Main", 1)]
        [InlineData(@" Main : () (returnValue : bit) = { local:bit=1/1 returnValue=local} ", "Main", 1)]
        [InlineData(@" Main : () (returnValue : bit) = { local:bit=1%1 returnValue=local+1} ", "Main", 1)]
        [InlineData(@" Main : () (returnValue : bit) = { local1,local2:bit=1 returnValue=local1/local2} ", "Main", 1)]
        public void CheckVoidWithLocalExpectsBit(string input, string entryPointName, byte expected)
        {
            Assert.True(InputVoidExpectsBitValue(CompileForTest(input, entryPointName), expected), $"Test {entryPointName},{input}");
        }

        [Theory]
        [InlineData(@"global : bit = 0 Main : () (returnValue : bit) = { returnValue=global } ", "Main", 0)]
        [InlineData(@"global : bit = 1 Main : () (returnValue : bit) = { returnValue=global } ", "Main", 1)]
        [InlineData(@"global : bit = +0 Main : () (returnValue : bit) = { returnValue=global } ", "Main", 0)]
        [InlineData(@"global : bit = +1 Main : () (returnValue : bit) = { returnValue=global } ", "Main", 1)]
        [InlineData(@"global : bit = 0+1 Main : () (returnValue : bit) = { returnValue=global } ", "Main", 1)]
        [InlineData(@"global : bit = 0+0 Main : () (returnValue : bit) = { returnValue=global } ", "Main", 0)]
        [InlineData(@"global : bit = 1-0 Main : () (returnValue : bit) = { returnValue=global } ", "Main", 1)]
        [InlineData(@"global : bit = 1*1 Main : () (returnValue : bit) = { returnValue=global } ", "Main", 1)]
        [InlineData(@"global : bit = 1/1 Main : () (returnValue : bit) = { returnValue=global } ", "Main", 1)]
        [InlineData(@"global : bit = 1%1 Main : () (returnValue : bit) = { returnValue=global } ", "Main", 0)]
        [InlineData(@"global1,global2 : bit = 0 Main : () (returnValue : bit) = { returnValue=global1+global2 } ", "Main", 0)]
        [InlineData(@"global1,global2 : bit = 1 Main : () (returnValue : bit) = { returnValue=global1+global2 } ", "Main", 0)]
        [InlineData(@"global1 : bit = 1 global2 : bit = 0 Main : () (returnValue : bit) = { returnValue=global1+global2 } ", "Main", 1)]
        [InlineData(@"global1 : bit = 0 global2 : bit = 1 Main : () (returnValue : bit) = { returnValue=global1+global2 } ", "Main", 1)]
        public void CheckGlobalsVoidExpectsBit(string input, string entryPointName, byte expected)
        {
            Assert.True(InputVoidExpectsBitValue(CompileForTest(input, entryPointName), expected), $"Test {entryPointName},{input}");
        }

        [Theory]
        [InlineData(@"global := 0 Main : () (returnValue : bit) = { returnValue=global } ", "Main", 0)]
        [InlineData(@"global := 1 Main : () (returnValue : bit) = { returnValue=global } ", "Main", 1)]
        [InlineData(@"global := +0 Main : () (returnValue : bit) = { returnValue=global } ", "Main", 0)]
        [InlineData(@"global := +1 Main : () (returnValue : bit) = { returnValue=global } ", "Main", 1)]
        [InlineData(@"global := 0+1 Main : () (returnValue : bit) = { returnValue=global } ", "Main", 1)]
        [InlineData(@"global := 0+0 Main : () (returnValue : bit) = { returnValue=global } ", "Main", 0)]
        [InlineData(@"global := 1-0 Main : () (returnValue : bit) = { returnValue=global } ", "Main", 1)]
        [InlineData(@"global := 1*1 Main : () (returnValue : bit) = { returnValue=global } ", "Main", 1)]
        [InlineData(@"global := 1/1 Main : () (returnValue : bit) = { returnValue=global } ", "Main", 1)]
        [InlineData(@"global := 1%1 Main : () (returnValue : bit) = { returnValue=global } ", "Main", 0)]
        [InlineData(@"global1,global2 := 0 Main : () (returnValue : bit) = { returnValue=global1+global2 } ", "Main", 0)]
        [InlineData(@"global1,global2 := 1 Main : () (returnValue : bit) = { returnValue=global1+global2 } ", "Main", 0)]
        [InlineData(@"global1 := 1 global2 := 0 Main : () (returnValue : bit) = { returnValue=global1+global2 } ", "Main", 1)]
        [InlineData(@"global1 := 0 global2 := 1 Main : () (returnValue : bit) = { returnValue=global1+global2 } ", "Main", 1)]
        public void CheckGlobalsAutoTypeVoidExpectsBit(string input, string entryPointName, byte expected)
        {
            Assert.True(InputVoidExpectsBitValue(CompileForTest(input, entryPointName), expected), $"Test {entryPointName},{input}");
        }


        [Theory]
        [InlineData(@"Main : () (returnValue : bit) = { local:=0 returnValue=local } ", "Main", 0)]
        [InlineData(@"Main : () (returnValue : bit) = { local:=1 returnValue=local } ", "Main", 1)]
        [InlineData(@"Main : () (returnValue : bit) = { local:=+0 returnValue=local } ", "Main", 0)]
        [InlineData(@"Main : () (returnValue : bit) = { local:=+1 returnValue=local } ", "Main", 1)]
        [InlineData(@"Main : () (returnValue : bit) = { local:=0+1 returnValue=local } ", "Main", 1)]
        [InlineData(@"Main : () (returnValue : bit) = { local:=0+0 returnValue=local } ", "Main", 0)]
        [InlineData(@"Main : () (returnValue : bit) = { local:=1-0 returnValue=local } ", "Main", 1)]
        [InlineData(@"Main : () (returnValue : bit) = { local:=1*1 returnValue=local } ", "Main", 1)]
        [InlineData(@"Main : () (returnValue : bit) = { local:=1/1 returnValue=local } ", "Main", 1)]
        [InlineData(@"Main : () (returnValue : bit) = { local:=1%1 returnValue=local } ", "Main", 0)]
        [InlineData(@"Main : () (returnValue : bit) = { local1,local2:=0 returnValue=local1+local2 } ", "Main", 0)]
        [InlineData(@"Main : () (returnValue : bit) = { local1,local2:=1 returnValue=local1+local2 } ", "Main", 0)]
        [InlineData(@"Main : () (returnValue : bit) = { local1:=1 local2:=0 returnValue=local1+local2 } ", "Main", 1)]
        [InlineData(@"Main : () (returnValue : bit) = { local1:=0 local2:=1 returnValue=local1+local2 } ", "Main", 1)]
        public void CheckLocalAutoTypeVoidExpectsBit(string input, string entryPointName, byte expected)
        {
            Assert.True(InputVoidExpectsBitValue(CompileForTest(input, entryPointName), expected), $"Test {entryPointName},{input}");
        }

        [Theory]
        [InlineData(@"Main : () (returnValue : bit) = { local:=0 local=1 returnValue=local } ", "Main", 1)]
        [InlineData(@"Main : () (returnValue : bit) = { local:=1 local=0 returnValue=local } ", "Main", 0)]
        [InlineData(@"Main : () (returnValue : bit) = { local:=0 local=1 returnValue=local-1 } ", "Main", 0)]
        [InlineData(@"Main : () (returnValue : bit) = { local:[2][1]bit=_ local[0]=1 local[1]=0 returnValue=local[1] } ", "Main", 0)]
        [InlineData(@"Main : () (returnValue : bit) = { local:[2][1]bit=_ local[0]=1 local[1]=0 returnValue=local[1]+local[0] } ", "Main", 1)]
        [InlineData(@"Main : () (returnValue : bit) = { local:{a:[1]bit b:[1]bit}=_ local.a=1 returnValue=local.a } ", "Main", 1)]
        public void CheckLocalReAssignAutoTypeVoidExpectsBit(string input, string entryPointName, byte expected)
        {
            Assert.True(InputVoidExpectsBitValue(CompileForTest(input, entryPointName), expected), $"Test {entryPointName},{input}");
        }

        [Theory]
        [InlineData(@" MainType : () (returnValue:[8]bit) Main : MainType = { returnValue=0 } ", "Main", 0)]
        [InlineData(@" Main : () (returnValue : [8]bit) = { returnValue=0 } ", "Main", 0)]
        [InlineData(@" Main : () (returnValue : [8]bit) = { returnValue=1 } ", "Main", 1)]
        [InlineData(@" Main : () (returnValue : [8]bit) = { returnValue=-1 } ", "Main", 255)]
        [InlineData(@" Main : () (returnValue : [8]bit) = { returnValue=-63 } ", "Main", 193)]
        [InlineData(@" Main : () (returnValue : [8]bit) = { returnValue=128 } ", "Main", 128)]
        [InlineData(@" Main : () (returnValue : [8]bit) = { returnValue=255 } ", "Main", 255)]
        public void CheckVoidExpects8Bit(string input, string entryPointName, byte expected)
        {
            Assert.True(InputVoidExpects8BitValue(CompileForTest(input, entryPointName), expected), $"Test {entryPointName},{input}");
        }

        [Theory]
        [InlineData(@" Main : () (returnValue : [-8]bit) = { returnValue=0 } ", "Main", 0)]
        [InlineData(@" Main : () (returnValue : [-8]bit) = { returnValue=1 } ", "Main", 1)]
        [InlineData(@" Main : () (returnValue : [-8]bit) = { returnValue=-1 } ", "Main", -1)]
        [InlineData(@" Main : () (returnValue : [-8]bit) = { returnValue=-63 } ", "Main", -63)]
        public void CheckVoidExpectsS8Bit(string input, string entryPointName, sbyte expected)
        {
            Assert.True(InputVoidExpectsS8BitValue(CompileForTest(input, entryPointName), expected), $"Test {entryPointName},{input}");
        }

        [Theory]
        [InlineData(@"Main : (a : bit) (returnValue : bit) = { returnValue=a}", "Main", 0, 0)]
        [InlineData(@"Main : (a : bit) (returnValue : bit) = { returnValue=a}", "Main", 1, 1)]
        [InlineData(@"Main : (a : bit) (returnValue : bit) = { returnValue=+a}", "Main", 0, 0)]
        [InlineData(@"Main : (a : bit) (returnValue : bit) = { returnValue=+a}", "Main", 1, 1)]
        [InlineData(@"Main : (a : bit) (returnValue : bit) = { returnValue=-a}", "Main", 0, 0)]
        [InlineData(@"Main : (a : bit) (returnValue : bit) = { returnValue=-a}", "Main", 1, 1)]
        public void CheckBitExpectsBit(string input, string entryPointName, byte ival, byte expected)
        {
            Assert.True(InputBitExpectsBitValue(CompileForTest(input, entryPointName), ival, expected), $"Test {entryPointName},{input}");
        }

        [Theory]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a+b}", "Main", 0, 0, 0)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a+b}", "Main", 0, 1, 1)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a+b}", "Main", 1, 0, 1)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a+b}", "Main", 1, 1, 0)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a-b}", "Main", 0, 0, 0)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a-b}", "Main", 0, 1, 1)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a-b}", "Main", 1, 0, 1)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a-b}", "Main", 1, 1, 0)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a*b}", "Main", 0, 0, 0)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a*b}", "Main", 0, 1, 0)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a*b}", "Main", 1, 0, 0)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a*b}", "Main", 1, 1, 1)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a/b}", "Main", 0, 1, 0)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a/b}", "Main", 1, 1, 1)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a%b}", "Main", 0, 1, 0)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a%b}", "Main", 1, 1, 0)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=(a+b*1)/1}", "Main", 1, 1, 0)]
        public void CheckBitBitExpectsBit(string input, string entryPointName, byte ival1, byte ival2, byte expected)
        {
            Assert.True(InputBitBitExpectsBitValue(CompileForTest(input, entryPointName), ival1, ival2, expected), $"Test {entryPointName},{input}");
        }

        [Theory]
        [InlineData(@"Main : (a : bit, b : bit) (returnB : bit , returnA : bit) = { returnB=b returnA=a}", "Main", 1, 0, 0, 1)]
        [InlineData(@"Main : (a : bit, b : bit) (returnB : bit , returnA : bit) = { returnB=b returnA=a}", "Main", 0, 1, 1, 0)]
        public void CheckBitBitExpectsBitBit(string input, string entryPointName, byte ival1, byte ival2, byte expected1, byte expected2)
        {
            Assert.True(InputBitBitExpectsBitBitValue(CompileForTest(input, entryPointName), ival1, ival2, expected1, expected2), $"Test {entryPointName},{input}");
        }

        [Theory]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { returnValue=a+b}", "Main", 128, 127, 255)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { returnValue=a+b}", "Main", 0, 127, 127)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { returnValue=a+b}", "Main", 55, 33, 88)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { returnValue=a+b}", "Main", 255, 255, 254)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { returnValue=a-b}", "Main", 128, 127, 1)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { returnValue=a-b}", "Main", 0, 127, 129)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { returnValue=a-b}", "Main", 55, 33, 22)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { returnValue=a-b}", "Main", 255, 255, 0)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { returnValue=a*b}", "Main", 2, 127, 254)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { returnValue=a*b}", "Main", 0, 127, 0)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { returnValue=a*b}", "Main", 55, 33, 23)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { returnValue=a*b}", "Main", 255, 255, 1)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { returnValue=a/b}", "Main", 128, 127, 1)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { returnValue=a/b}", "Main", 0, 127, 0)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { returnValue=a/b}", "Main", 55, 33, 1)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { returnValue=a/b}", "Main", 255, 255, 1)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { returnValue=a%b}", "Main", 128, 127, 1)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { returnValue=a%b}", "Main", 0, 127, 0)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { returnValue=a%b}", "Main", 55, 33, 22)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { returnValue=a%b}", "Main", 255, 255, 0)]
        public void Check8Bit8BitExpects8Bit(string input, string entryPointName, byte ival1, byte ival2, byte expected)
        {
            Assert.True(Input8Bit8BitExpects8BitValue(CompileForTest(input, entryPointName), ival1, ival2, expected), $"Test {entryPointName},{input}");
        }

        [Theory]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { returnValue=a+b}", "Main", -128, 127, -1)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { returnValue=a+b}", "Main", -5, -4, -9)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { returnValue=a+b}", "Main", 55, 33, 88)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { returnValue=a+b}", "Main", 127, -127, 0)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { returnValue=a-b}", "Main", -128, 127, 1)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { returnValue=a-b}", "Main", 0, 127, -127)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { returnValue=a-b}", "Main", 55, 33, 22)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { returnValue=a-b}", "Main", 127, -127, -2)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { returnValue=a*b}", "Main", -2, 64, -128)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { returnValue=a*b}", "Main", 0, -127, 0)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { returnValue=a*b}", "Main", 55, -33, -23)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { returnValue=a*b}", "Main", 127, 127, 1)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { returnValue=a/b}", "Main", -127, -127, 1)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { returnValue=a/b}", "Main", 0, -127, 0)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { returnValue=a/b}", "Main", 55, -33, -1)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { returnValue=a/b}", "Main", -55, 33, -1)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { returnValue=a/b}", "Main", 127, 127, 1)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { returnValue=a%b}", "Main", -127, -127, 0)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { returnValue=a%b}", "Main", 0, 127, 0)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { returnValue=a%b}", "Main", -55, -33, -22)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { returnValue=a%b}", "Main", 127, 127, 0)]
        public void CheckS8BitS8BitExpectsS8Bit(string input, string entryPointName, sbyte ival1, sbyte ival2, sbyte expected)
        {
            Assert.True(InputS8BitS8BitExpectsS8BitValue(CompileForTest(input, entryPointName), ival1, ival2, expected), $"Test {entryPointName},{input},{ival1},{ival2},{expected}");
        }


        [Theory]
        [InlineData(@"Main : (a : *[8]bit) (returnValue : [8]bit) = { returnValue=a[0] }", "Main", new byte[] { 0, 1 }, 0)]
        [InlineData(@"Main : (a : *[8]bit) (returnValue : [8]bit) = { returnValue=a[1] }", "Main", new byte[] { 0, 1 }, 1)]
        [InlineData(@"Main : (a : *[8]bit) (returnValue : [8]bit) = { returnValue=a[0] }", "Main", new byte[] { 15, 23 }, 15)]
        [InlineData(@"Main : (a : *[8]bit) (returnValue : [8]bit) = { returnValue=a[1] }", "Main", new byte[] { 15, 23 }, 23)]
        [InlineData(@"Main : (a : *[8]bit) (returnValue : [8]bit) = { returnValue=a[1+1+1] }", "Main", new byte[] { 15, 23, 55, 66 }, 66)]
        public void CheckSubscriptConst(string input, string entryPointName, byte[] inputArray, byte expected)
        {
            Assert.True(InputBytePointerToArrayExpectsByteValue(CompileForTest(input, entryPointName), inputArray, expected), $"Test {entryPointName},{input},{inputArray},{expected}");
        }

        [Theory]
        [InlineData(@"Main : (a : *[8]bit,b : [8]bit) (returnValue : [8]bit) = { returnValue=a[b] }", "Main", new byte[] { 0, 1 }, 0, 0)]
        [InlineData(@"Main : (a : *[8]bit,b : [8]bit) (returnValue : [8]bit) = { returnValue=a[b] }", "Main", new byte[] { 0, 1 }, 1, 1)]
        [InlineData(@"Main : (a : *[8]bit,b : [8]bit) (returnValue : [8]bit) = { returnValue=a[b] }", "Main", new byte[] { 15, 23 }, 0, 15)]
        [InlineData(@"Main : (a : *[8]bit,b : [8]bit) (returnValue : [8]bit) = { returnValue=a[b] }", "Main", new byte[] { 15, 23 }, 1, 23)]
        [InlineData(@"Main : (a : *[8]bit,b : [8]bit) (returnValue : [8]bit) = { returnValue=a[b-1] }", "Main", new byte[] { 15, 23 }, 1, 15)]
        public void CheckSubscriptDynamic(string input, string entryPointName, byte[] inputArray, byte index, byte expected)
        {
            Assert.True(InputBytePointerToArrayByteExpectsByteValue(CompileForTest(input, entryPointName), inputArray, index, expected), $"Test {entryPointName},{input},{inputArray},{index},{expected}");
        }

        [Theory]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { x,y:[8]bit=0 for x=a..b { y=y+1 } returnValue=y}", "Main", 0, 0, 0)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { x,y:[8]bit=0 for x=a..b { y=y+1 } returnValue=y}", "Main", 0, 1, 1)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { x,y:[8]bit=0 for x=a..b { y=y+1 } returnValue=y}", "Main", 0, 99, 99)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { x,y:[8]bit=0 for x=a..b { y=y+1 } returnValue=y}", "Main", 10, 11, 1)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { x,y:[8]bit=0 for x=a..b { y=y+2 } returnValue=y}", "Main", 10, 11, 2)]
        public void CheckForIntRangeLoop(string input, string entryPointName, byte ival1, byte ival2, byte expected)
        {
            Assert.True(Input8Bit8BitExpects8BitValue(CompileForTest(input, entryPointName), ival1, ival2, expected), $"Test {entryPointName},{input},{ival1},{ival2},{expected}");
        }
        
        [Theory]
        [InlineData(@"Main:(a:[8]bit,b:[8]bit)(returnValue:[8]bit)={ x,y,z:[8]bit=0 for x=a..b{for y=a..b{z=z+1}}returnValue=z}", "Main", 0, 0, 0)]
        [InlineData(@"Main:(a:[8]bit,b:[8]bit)(returnValue:[8]bit)={ x,y,z:[8]bit=0 for x=a..b{for y=a..b{z=z+1}}returnValue=z}", "Main", 0, 1, 1)]
        [InlineData(@"Main:(a:[8]bit,b:[8]bit)(returnValue:[8]bit)={ x,y,z:[8]bit=0 for x=a..b{for y=a..b{z=z+1}}returnValue=z}", "Main", 0, 2, 4)]
        [InlineData(@"Main:(a:[8]bit,b:[8]bit)(returnValue:[8]bit)={ x,y,z:[8]bit=0 for x=a..b{for y=a..b{z=z+1}}returnValue=z}", "Main", 10, 11, 1)]
        [InlineData(@"Main:(a:[8]bit,b:[8]bit)(returnValue:[8]bit)={ x,y,z:[8]bit=0 for x=a..b{for y=a..b{z=z+1}}returnValue=z}", "Main", 10, 12, 4)]
        [InlineData(@"Main:(a:[8]bit,b:[8]bit)(returnValue:[8]bit)={ x,y,z,w:[8]bit=0 for x=a..b{for y=a..b{for z=a..b{w=w+1}}}returnValue=w}", "Main", 0, 2, 8)]
        public void CheckNestedForIntRangeLoop(string input, string entryPointName, byte ival1, byte ival2, byte expected)
        {
            Assert.True(Input8Bit8BitExpects8BitValue(CompileForTest(input, entryPointName), ival1, ival2, expected), $"Test {entryPointName},{input},{ival1},{ival2},{expected}");
        }

        [Theory]
        [InlineData(@"Bob:(a:bit,b:bit)(out:bit)={out=a+b}Main:(a:bit,b:bit)(out:bit)={out=Bob(a,b)}", "Main", 0, 0, 0)]
        [InlineData(@"Bob:(a:bit,b:bit)(out:bit)={out=a+b}Main:(a:bit,b:bit)(out:bit)={out=Bob(a,b)}", "Main", 0, 1, 1)]
        [InlineData(@"Bob:(a:bit,b:bit)(out:bit)={out=a+b}Main:(a:bit,b:bit)(out:bit)={out=Bob(a,b)}", "Main", 1, 0, 1)]
        [InlineData(@"Bob:(a:bit,b:bit)(out:bit)={out=a+b}Main:(a:bit,b:bit)(out:bit)={out=Bob(a,b).out}", "Main", 0, 0, 0)]
        [InlineData(@"Bob:(a:bit,b:bit)(out:bit)={out=a+b}Main:(a:bit,b:bit)(out:bit)={out=Bob(a,b).out}", "Main", 0, 1, 1)]
        [InlineData(@"Bob:(a:bit,b:bit)(out:bit)={out=a+b}Main:(a:bit,b:bit)(out:bit)={out=Bob(a,b).out}", "Main", 1, 0, 1)]
        public void CheckSimpleFunctionCall(string input, string entryPointName, byte ival1, byte ival2, byte expected)
        {
            Assert.True(InputBitBitExpectsBitValue(CompileForTest(input, entryPointName), ival1, ival2, expected), $"Test {entryPointName},{input},{ival1},{ival2},{expected}");
        }

        const string MultiReturnCall = @"
Bob:(a:bit,b:bit)(out1:bit,out2:bit)=
{
    out1=b 
    out2=a
}
Main:(a:bit,b:bit)(out:bit)=
{
    result:=Bob(a,b) 
    out=result.out1+result.out2
}";

        [Theory]
        [InlineData(MultiReturnCall, "Main", 0, 0, 0)]
        [InlineData(MultiReturnCall, "Main", 0, 1, 1)]
        [InlineData(MultiReturnCall, "Main", 1, 0, 1)]
        public void CheckMultiReturnCall(string input, string entryPointName, byte ival1, byte ival2, byte expected)
        {
            Assert.True(InputBitBitExpectsBitValue(CompileForTest(input, entryPointName), ival1, ival2, expected), $"Test {entryPointName},{input},{ival1},{ival2},{expected}");
        }

        [Theory]
        [InlineData("Main:()(out:bit)={local:{nest:{a:bit}}=_ local.nest.a=0 out=local.nest.a}","Main",0)]
        [InlineData("Main:()(out:bit)={local:{nest:{a:bit}}=_ local.nest.a=1 out=local.nest.a}","Main",1)]
        [InlineData("Main:()(out:bit)={local:{nest:{deepl:{a:bit}deepr:{a:bit}}}=_ local.nest.deepl.a,local.nest.deepr.a=0 out=local.nest.deepr.a}","Main",0)]
        [InlineData("Main:()(out:bit)={local:{nest:{deepl:{a:bit}deepr:{a:bit}}}=_ local.nest.deepl.a,local.nest.deepr.a=1 out=local.nest.deepr.a}","Main",1)]
        public void CheckAssignNestedStruct(string input, string entryPointName, byte expected)
        {
            Assert.True(InputVoidExpectsBitValue(CompileForTest(input, entryPointName), expected), $"Test {entryPointName},{input},{expected}");
        }

        public IntPtr CompileForTest(string input, string entryPointName)
        {
            var messages = new CompilerMessages(true, true, false);
            var tokenise = new HumphreyTokeniser(messages);
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens, messages);
            var parsed = parser.File();
            var compiler = new CompilationUnit("test", messages);
            foreach (var def in parsed)
            {
                def.Compile(compiler);
            }

            if (messages.HasErrors)
            {
                throw new Exception($"{messages.Dump()}");
            }

            return compiler.JitMethod(entryPointName);
        }

        delegate void InputVoidOutputBit(byte* returnVal);

        public static bool InputVoidExpectsBitValue(IntPtr ee, byte expected)
        {
            var func = Marshal.GetDelegateForFunctionPointer<InputVoidOutputBit>(ee);
            byte returnValue;
            func(&returnValue);
            return returnValue == expected;
        }
        delegate void InputVoidOutput8Bit(byte* returnVal);

        public static bool InputVoidExpects8BitValue(IntPtr ee, byte expected)
        {
            var func = Marshal.GetDelegateForFunctionPointer<InputVoidOutput8Bit>(ee);
            byte returnValue;
            func(&returnValue);
            return returnValue == expected;
        }

        delegate void InputVoidOutputS8Bit(sbyte* returnVal);

        public static bool InputVoidExpectsS8BitValue(IntPtr ee, sbyte expected)
        {
            var func = Marshal.GetDelegateForFunctionPointer<InputVoidOutputS8Bit>(ee);
            sbyte returnValue;
            func(&returnValue);
            return returnValue == expected;
        }

        delegate void InputBitOutputBit(byte inputVal, byte* returnVal);

        public static bool InputBitExpectsBitValue(IntPtr ee, byte input, byte expected)
        {
            var func = Marshal.GetDelegateForFunctionPointer<InputBitOutputBit>(ee);
            byte returnValue;
            func(input, &returnValue);
            return returnValue == expected;
        }

        delegate void InputBitBitOutputBit(byte inputVal1, byte inputVal2, byte* returnVal);

        public static bool InputBitBitExpectsBitValue(IntPtr ee, byte input1, byte input2, byte expected)
        {
            var func = Marshal.GetDelegateForFunctionPointer<InputBitBitOutputBit>(ee);
            byte returnValue;
            func(input1, input2, &returnValue);
            return returnValue == expected;
        }

        delegate void Input8Bit8BitOutput8Bit(byte inputVal1, byte inputVal2, byte* returnVal);

        public static bool Input8Bit8BitExpects8BitValue(IntPtr ee, byte input1, byte input2, byte expected)
        {
            var func = Marshal.GetDelegateForFunctionPointer<Input8Bit8BitOutput8Bit>(ee);
            byte returnValue;
            func(input1, input2, &returnValue);
            return returnValue == expected;
        }

        delegate void InputS8BitS8BitOutputS8Bit(sbyte inputVal1, sbyte inputVal2, sbyte* returnVal);

        public static bool InputS8BitS8BitExpectsS8BitValue(IntPtr ee, sbyte input1, sbyte input2, sbyte expected)
        {
            var func = Marshal.GetDelegateForFunctionPointer<InputS8BitS8BitOutputS8Bit>(ee);
            sbyte returnValue;
            func(input1, input2, &returnValue);
            return returnValue == expected;
        }

        delegate void InputPointerToByteArrayExpectsByte(byte* inputVal, byte* returnVal);

        public static bool InputBytePointerToArrayExpectsByteValue(IntPtr ee, byte[] input, byte expected)
        {
            var func = Marshal.GetDelegateForFunctionPointer<InputPointerToByteArrayExpectsByte>(ee);
            byte returnValue;
            fixed (byte* ptr = input)
            {
                func(ptr, &returnValue);
            }
            return returnValue == expected;
        }
        delegate void InputPointerToByteArrayByteExpectsByte(byte* inputVal1, byte inputVal2, byte* returnVal);

        public static bool InputBytePointerToArrayByteExpectsByteValue(IntPtr ee, byte[] input1, byte input2, byte expected)
        {
            var func = Marshal.GetDelegateForFunctionPointer<InputPointerToByteArrayByteExpectsByte>(ee);
            byte returnValue;
            fixed (byte* ptr = input1)
            {
                func(ptr, input2, &returnValue);
            }
            return returnValue == expected;
        }


        delegate void InputBitBitOutputBitBit(byte inputVal1, byte inputVal2, byte* returnVal1, byte* returnVal2);

        public static bool InputBitBitExpectsBitBitValue(IntPtr ee, byte input1, byte input2, byte expected1, byte expected2)
        {
            var func = Marshal.GetDelegateForFunctionPointer<InputBitBitOutputBitBit>(ee);
            byte returnValue1, returnValue2;
            func(input1, input2, &returnValue1, &returnValue2);
            return returnValue1 == expected1 && returnValue2 == expected2;
        }
    }
}

