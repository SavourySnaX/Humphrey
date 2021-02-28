using Xunit;

using Humphrey.FrontEnd;
using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;

namespace Humphrey.Backend.tests
{
    public unsafe class JitTests
    {
        [Theory]
        [InlineData(@" Main : () (returnValue : bit) = { returnValue = 0; } ", "Main", 0)]
        [InlineData(@" Main : () (returnValue : bit) = { returnValue = 1; } ", "Main", 1)]
        [InlineData(@" Main : () (returnValue : bit) = { returnValue = +1; } ", "Main", 1)]
        [InlineData(@" Main : () (returnValue : bit) = { returnValue = 1+0; } ", "Main", 1)]
        [InlineData(@" Main : () (returnValue : bit) = { returnValue = 1-1; } ", "Main", 0)]
        [InlineData(@" Main : () (returnValue : bit) = { returnValue = 1*1; } ", "Main", 1)]
        [InlineData(@" Main : () (returnValue : bit) = { returnValue = 1/1; } ", "Main", 1)]
        [InlineData(@" Main : () (returnValue : bit) = { returnValue = 1%1; } ", "Main", 0)]
        public void CheckVoidExpectsBit(string input, string entryPointName, byte expected)
        {
            Assert.True(InputVoidExpectsBitValue(CompileForTest(input, entryPointName), expected), $"Test {entryPointName},{input}");
        }

        [Theory]
        [InlineData(@" Main : () (returnValue : bit) = { local:bit=0; returnValue=local;} ", "Main", 0)]
        [InlineData(@" Main : () (returnValue : bit) = { local:bit=+1; returnValue=local;} ", "Main", 1)]
        [InlineData(@" Main : () (returnValue : bit) = { local:bit=1+0; returnValue=local;} ", "Main", 1)]
        [InlineData(@" Main : () (returnValue : bit) = { local:bit= 1-1;  returnValue=local;} ", "Main", 0)]
        [InlineData(@" Main : () (returnValue : bit) = { local:bit=1*1; returnValue=local;} ", "Main", 1)]
        [InlineData(@" Main : () (returnValue : bit) = { local:bit=1/1; returnValue=local;} ", "Main", 1)]
        [InlineData(@" Main : () (returnValue : bit) = { local:bit=1%1; returnValue=local+1;} ", "Main", 1)]
        [InlineData(@" Main : () (returnValue : bit) = { local1,local2:bit=1; returnValue=local1/local2;} ", "Main", 1)]
        public void CheckVoidWithLocalExpectsBit(string input, string entryPointName, byte expected)
        {
            Assert.True(InputVoidExpectsBitValue(CompileForTest(input, entryPointName), expected), $"Test {entryPointName},{input}");
        }

        [Theory]
        [InlineData(@"global : bit = 0 Main : () (returnValue : bit) = { returnValue=global; } ", "Main", 0)]
        [InlineData(@"global : bit = 1 Main : () (returnValue : bit) = { returnValue=global; } ", "Main", 1)]
        [InlineData(@"global : bit = +0 Main : () (returnValue : bit) = { returnValue=global; } ", "Main", 0)]
        [InlineData(@"global : bit = +1 Main : () (returnValue : bit) = { returnValue=global; } ", "Main", 1)]
        [InlineData(@"global : bit = 0+1 Main : () (returnValue : bit) = { returnValue=global; } ", "Main", 1)]
        [InlineData(@"global : bit = 0+0 Main : () (returnValue : bit) = { returnValue=global; } ", "Main", 0)]
        [InlineData(@"global : bit = 1-0 Main : () (returnValue : bit) = { returnValue=global; } ", "Main", 1)]
        [InlineData(@"global : bit = 1*1 Main : () (returnValue : bit) = { returnValue=global; } ", "Main", 1)]
        [InlineData(@"global : bit = 1/1 Main : () (returnValue : bit) = { returnValue=global; } ", "Main", 1)]
        [InlineData(@"global : bit = 1%1 Main : () (returnValue : bit) = { returnValue=global; } ", "Main", 0)]
        [InlineData(@"global1,global2 : bit = 0 Main : () (returnValue : bit) = { returnValue=global1+global2; } ", "Main", 0)]
        [InlineData(@"global1,global2 : bit = 1 Main : () (returnValue : bit) = { returnValue=global1+global2; } ", "Main", 0)]
        [InlineData(@"global1 : bit = 1 global2 : bit = 0 Main : () (returnValue : bit) = { returnValue=global1+global2; } ", "Main", 1)]
        [InlineData(@"global1 : bit = 0 global2 : bit = 1 Main : () (returnValue : bit) = { returnValue=global1+global2; } ", "Main", 1)]
        public void CheckGlobalsVoidExpectsBit(string input, string entryPointName, byte expected)
        {
            Assert.True(InputVoidExpectsBitValue(CompileForTest(input, entryPointName), expected), $"Test {entryPointName},{input}");
        }

        [Theory]
        [InlineData(@"global := 0 Main : () (returnValue : bit) = { returnValue=global; } ", "Main", 0)]
        [InlineData(@"global := 1 Main : () (returnValue : bit) = { returnValue=global; } ", "Main", 1)]
        [InlineData(@"global := +0 Main : () (returnValue : bit) = { returnValue=global; } ", "Main", 0)]
        [InlineData(@"global := +1 Main : () (returnValue : bit) = { returnValue=global; } ", "Main", 1)]
        [InlineData(@"global := 0+1 Main : () (returnValue : bit) = { returnValue=global; } ", "Main", 1)]
        [InlineData(@"global := 0+0 Main : () (returnValue : bit) = { returnValue=global; } ", "Main", 0)]
        [InlineData(@"global := 1-0 Main : () (returnValue : bit) = { returnValue=global; } ", "Main", 1)]
        [InlineData(@"global := 1*1 Main : () (returnValue : bit) = { returnValue=global; } ", "Main", 1)]
        [InlineData(@"global := 1/1 Main : () (returnValue : bit) = { returnValue=global; } ", "Main", 1)]
        [InlineData(@"global := 1%1 Main : () (returnValue : bit) = { returnValue=global; } ", "Main", 0)]
        [InlineData(@"global1,global2 := 0 Main : () (returnValue : bit) = { returnValue=global1+global2; } ", "Main", 0)]
        [InlineData(@"global1,global2 := 1 Main : () (returnValue : bit) = { returnValue=global1+global2; } ", "Main", 0)]
        [InlineData(@"global1 := 1 global2 := 0 Main : () (returnValue : bit) = { returnValue=global1+global2; } ", "Main", 1)]
        [InlineData(@"global1 := 0 global2 := 1 Main : () (returnValue : bit) = { returnValue=global1+global2; } ", "Main", 1)]
        public void CheckGlobalsAutoTypeVoidExpectsBit(string input, string entryPointName, byte expected)
        {
            Assert.True(InputVoidExpectsBitValue(CompileForTest(input, entryPointName), expected), $"Test {entryPointName},{input}");
        }


        [Theory]
        [InlineData(@"Main : () (returnValue : bit) = { local:=0; returnValue=local; } ", "Main", 0)]
        [InlineData(@"Main : () (returnValue : bit) = { local:=1; returnValue=local; } ", "Main", 1)]
        [InlineData(@"Main : () (returnValue : bit) = { local:=+0; returnValue=local; } ", "Main", 0)]
        [InlineData(@"Main : () (returnValue : bit) = { local:=+1; returnValue=local; } ", "Main", 1)]
        [InlineData(@"Main : () (returnValue : bit) = { local:=0+1; returnValue=local; } ", "Main", 1)]
        [InlineData(@"Main : () (returnValue : bit) = { local:=0+0; returnValue=local; } ", "Main", 0)]
        [InlineData(@"Main : () (returnValue : bit) = { local:=1-0; returnValue=local; } ", "Main", 1)]
        [InlineData(@"Main : () (returnValue : bit) = { local:=1*1; returnValue=local; } ", "Main", 1)]
        [InlineData(@"Main : () (returnValue : bit) = { local:=1/1; returnValue=local; } ", "Main", 1)]
        [InlineData(@"Main : () (returnValue : bit) = { local:=1%1; returnValue=local; } ", "Main", 0)]
        [InlineData(@"Main : () (returnValue : bit) = { local1,local2:=0; returnValue=local1+local2; } ", "Main", 0)]
        [InlineData(@"Main : () (returnValue : bit) = { local1,local2:=1; returnValue=local1+local2; } ", "Main", 0)]
        [InlineData(@"Main : () (returnValue : bit) = { local1:=1; local2:=0; returnValue=local1+local2; } ", "Main", 1)]
        [InlineData(@"Main : () (returnValue : bit) = { local1:=0; local2:=1; returnValue=local1+local2; } ", "Main", 1)]
        public void CheckLocalAutoTypeVoidExpectsBit(string input, string entryPointName, byte expected)
        {
            Assert.True(InputVoidExpectsBitValue(CompileForTest(input, entryPointName), expected), $"Test {entryPointName},{input}");
        }

        [Theory]
        [InlineData(@"Main : () (returnValue : bit) = { local:=0; local=1; returnValue=local; } ", "Main", 1)]
        [InlineData(@"Main : () (returnValue : bit) = { local:=1; local=0; returnValue=local; } ", "Main", 0)]
        [InlineData(@"Main : () (returnValue : bit) = { local:=0; local=1; returnValue=local-1; } ", "Main", 0)]
        [InlineData(@"Main : () (returnValue : bit) = { local:[2][1]bit=_; local[0]=1; local[1]=0; returnValue=local[1]; } ", "Main", 0)]
        [InlineData(@"Main : () (returnValue : bit) = { local:[2][1]bit=_; local[0]=1; local[1]=0; returnValue=local[1]+local[0]; } ", "Main", 1)]
        [InlineData(@"Main : () (returnValue : bit) = { local:{a:[1]bit b:[1]bit}=_; local.a=1; returnValue=local.a; } ", "Main", 1)]
        public void CheckLocalReAssignAutoTypeVoidExpectsBit(string input, string entryPointName, byte expected)
        {
            Assert.True(InputVoidExpectsBitValue(CompileForTest(input, entryPointName), expected), $"Test {entryPointName},{input}");
        }

        [Theory]
        [InlineData(@" MainType : () (returnValue:[8]bit) Main : MainType = { returnValue=0; } ", "Main", 0)]
        [InlineData(@" Main : () (returnValue : [8]bit) = { returnValue=0; } ", "Main", 0)]
        [InlineData(@" Main : () (returnValue : [8]bit) = { returnValue=1; } ", "Main", 1)]
        [InlineData(@" Main : () (returnValue : [8]bit) = { returnValue=-1; } ", "Main", 255)]
        [InlineData(@" Main : () (returnValue : [8]bit) = { returnValue=-63; } ", "Main", 193)]
        [InlineData(@" Main : () (returnValue : [8]bit) = { returnValue=128; } ", "Main", 128)]
        [InlineData(@" Main : () (returnValue : [8]bit) = { returnValue=255; } ", "Main", 255)]
        public void CheckVoidExpects8Bit(string input, string entryPointName, byte expected)
        {
            Assert.True(InputVoidExpects8BitValue(CompileForTest(input, entryPointName), expected), $"Test {entryPointName},{input}");
        }

        [Theory]
        [InlineData(@" Main : () (returnValue : [-8]bit) = { returnValue=0; } ", "Main", 0)]
        [InlineData(@" Main : () (returnValue : [-8]bit) = { returnValue=1; } ", "Main", 1)]
        [InlineData(@" Main : () (returnValue : [-8]bit) = { returnValue=-1; } ", "Main", -1)]
        [InlineData(@" Main : () (returnValue : [-8]bit) = { returnValue=-63; } ", "Main", -63)]
        public void CheckVoidExpectsS8Bit(string input, string entryPointName, sbyte expected)
        {
            Assert.True(InputVoidExpectsS8BitValue(CompileForTest(input, entryPointName), expected), $"Test {entryPointName},{input}");
        }

        [Theory]
        [InlineData(@"Main : (a : bit) (returnValue : bit) = { returnValue=a;}", "Main", 0, 0)]
        [InlineData(@"Main : (a : bit) (returnValue : bit) = { returnValue=a;}", "Main", 1, 1)]
        [InlineData(@"Main : (a : bit) (returnValue : bit) = { returnValue=+a;}", "Main", 0, 0)]
        [InlineData(@"Main : (a : bit) (returnValue : bit) = { returnValue=+a;}", "Main", 1, 1)]
        [InlineData(@"Main : (a : bit) (returnValue : bit) = { returnValue=-a;}", "Main", 0, 0)]
        [InlineData(@"Main : (a : bit) (returnValue : bit) = { returnValue=-a;}", "Main", 1, 1)]
        [InlineData(@"Main : (a : bit) (returnValue : bit) = { returnValue=!a;}", "Main", 0, 1)]
        [InlineData(@"Main : (a : bit) (returnValue : bit) = { returnValue=!a;}", "Main", 1, 0)]
        [InlineData(@"Main : (a : bit) (returnValue : bit) = { returnValue=~a;}", "Main", 0, 1)]
        [InlineData(@"Main : (a : bit) (returnValue : bit) = { returnValue=~a;}", "Main", 1, 0)]
        public void CheckBitExpectsBit(string input, string entryPointName, byte ival, byte expected)
        {
            Assert.True(InputBitExpectsBitValue(CompileForTest(input, entryPointName), ival, expected), $"Test {entryPointName},{input}");
        }

        [Theory]
        [InlineData(@"Main : (a : bit) (out : bit) = { a=a+1; out=a;}", "Main", 0, 1)]
        public void CheckReadWriteInput(string input, string entryPointName, byte ival, byte expected)
        {
            Assert.True(InputBitExpectsBitValue(CompileForTest(input, entryPointName), ival, expected), $"Test {entryPointName},{input}");
        }
        
        [Theory]
        [InlineData(@"Main : (a : bit) (out : bit) = { out=a; out=out+1;}", "Main", 0, 1)]
        public void CheckReadWriteOutput(string input, string entryPointName, byte ival, byte expected)
        {
            Assert.True(InputBitExpectsBitValue(CompileForTest(input, entryPointName), ival, expected), $"Test {entryPointName},{input}");
        }

        [Theory]
        [InlineData(@"Main : (a : bit) (outA : bit, outB : bit) = { outB=a; outA=++outB;}", "Main", 0, 1, 1)]
        [InlineData(@"Main : (a : bit) (outA : bit, outB : bit) = { outB=a; outA=outB++;}", "Main", 0, 0, 1)]
        [InlineData(@"Main : (a : bit) (outA : bit, outB : bit) = { outB=a; outA=--outB;}", "Main", 1, 0, 0)]
        [InlineData(@"Main : (a : bit) (outA : bit, outB : bit) = { outB=a; outA=outB--;}", "Main", 1, 1, 0)]
        public void CheckPrePostBitIncDec(string input, string entryPointName, byte ival, byte expectedA, byte expectedB)
        {
            Assert.True(InputBitExpectsBitBitValue(CompileForTest(input, entryPointName), ival, expectedA, expectedB), $"Test {entryPointName},{input},{ival},{expectedA},{expectedB}");
        }

        [Theory]
        [InlineData(@"Main : (a : [8]bit) (outA : [8]bit, outB : [8]bit) = { outB=a; outA=++outB;}", "Main", 5, 6, 6)]
        [InlineData(@"Main : (a : [8]bit) (outA : [8]bit, outB : [8]bit) = { outB=a; outA=outB++;}", "Main", 23, 23, 24)]
        [InlineData(@"Main : (a : [8]bit) (outA : [8]bit, outB : [8]bit) = { outB=a; outA=--outB;}", "Main", 191, 190, 190)]
        [InlineData(@"Main : (a : [8]bit) (outA : [8]bit, outB : [8]bit) = { outB=a; outA=outB--;}", "Main", 255, 255, 254)]
        [InlineData(@"Main : (a : [8]bit) (outA : [8]bit, outB : [8]bit) = { outB=a; outA=(++outB)*2;}", "Main", 5, 12, 6)]
        [InlineData(@"Main : (a : [8]bit) (outA : [8]bit, outB : [8]bit) = { outB=a; outA=2*(outB++);}", "Main", 23, 46, 24)]
        public void CheckPrePostBytIncDec(string input, string entryPointName, byte ival, byte expectedA, byte expectedB)
        {
            Assert.True(InputByteExpectsByteByteValue(CompileForTest(input, entryPointName), ival, expectedA, expectedB), $"Test {entryPointName},{input},{ival},{expectedA},{expectedB}");
        }

        [Theory]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a+b;}", "Main", 0, 0, 0)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a+b;}", "Main", 0, 1, 1)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a+b;}", "Main", 1, 0, 1)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a+b;}", "Main", 1, 1, 0)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a-b;}", "Main", 0, 0, 0)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a-b;}", "Main", 0, 1, 1)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a-b;}", "Main", 1, 0, 1)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a-b;}", "Main", 1, 1, 0)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a*b;}", "Main", 0, 0, 0)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a*b;}", "Main", 0, 1, 0)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a*b;}", "Main", 1, 0, 0)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a*b;}", "Main", 1, 1, 1)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a/b;}", "Main", 0, 1, 0)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a/b;}", "Main", 1, 1, 1)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a%b;}", "Main", 0, 1, 0)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a%b;}", "Main", 1, 1, 0)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=(a+b*1)/1;}", "Main", 1, 1, 0)]
        public void CheckBitBitExpectsBit(string input, string entryPointName, byte ival1, byte ival2, byte expected)
        {
            Assert.True(InputBitBitExpectsBitValue(CompileForTest(input, entryPointName), ival1, ival2, expected), $"Test {entryPointName},{input}");
        }

        [Theory]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a&&b;}", "Main", 0, 0, 0)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a&&b;}", "Main", 0, 1, 0)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a&&b;}", "Main", 1, 0, 0)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a&&b;}", "Main", 1, 1, 1)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a||b;}", "Main", 0, 0, 0)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a||b;}", "Main", 0, 1, 1)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a||b;}", "Main", 1, 0, 1)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a||b;}", "Main", 1, 1, 1)]
        public void CheckLogicalBinaryOperators(string input, string entryPointName, byte ival1, byte ival2, byte expected)
        {
            Assert.True(InputBitBitExpectsBitValue(CompileForTest(input, entryPointName), ival1, ival2, expected), $"Test {entryPointName},{input}");
        }

        [Theory]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a&b;}", "Main", 0, 0, 0)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a&b;}", "Main", 0, 1, 0)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a&b;}", "Main", 1, 0, 0)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a&b;}", "Main", 1, 1, 1)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a|b;}", "Main", 0, 0, 0)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a|b;}", "Main", 0, 1, 1)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a|b;}", "Main", 1, 0, 1)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a|b;}", "Main", 1, 1, 1)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a^b;}", "Main", 0, 0, 0)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a^b;}", "Main", 0, 1, 1)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a^b;}", "Main", 1, 0, 1)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { returnValue=a^b;}", "Main", 1, 1, 0)]
        public void CheckBinaryBinaryOperators(string input, string entryPointName, byte ival1, byte ival2, byte expected)
        {
            Assert.True(InputBitBitExpectsBitValue(CompileForTest(input, entryPointName), ival1, ival2, expected), $"Test {entryPointName},{input}");
        }


        [Theory]
        [InlineData(@"Main : (a : bit, b : bit) (returnB : bit , returnA : bit) = { returnB=b; returnA=a;}", "Main", 1, 0, 0, 1)]
        [InlineData(@"Main : (a : bit, b : bit) (returnB : bit , returnA : bit) = { returnB=b; returnA=a;}", "Main", 0, 1, 1, 0)]
        public void CheckBitBitExpectsBitBit(string input, string entryPointName, byte ival1, byte ival2, byte expected1, byte expected2)
        {
            Assert.True(InputBitBitExpectsBitBitValue(CompileForTest(input, entryPointName), ival1, ival2, expected1, expected2), $"Test {entryPointName},{input}");
        }

        [Theory]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { returnValue=a+b;}", "Main", 128, 127, 255)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { returnValue=a+b;}", "Main", 0, 127, 127)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { returnValue=a+b;}", "Main", 55, 33, 88)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { returnValue=a+b;}", "Main", 255, 255, 254)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { returnValue=a-b;}", "Main", 128, 127, 1)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { returnValue=a-b;}", "Main", 0, 127, 129)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { returnValue=a-b;}", "Main", 55, 33, 22)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { returnValue=a-b;}", "Main", 255, 255, 0)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { returnValue=a*b;}", "Main", 2, 127, 254)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { returnValue=a*b;}", "Main", 0, 127, 0)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { returnValue=a*b;}", "Main", 55, 33, 23)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { returnValue=a*b;}", "Main", 255, 255, 1)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { returnValue=a/b;}", "Main", 128, 127, 1)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { returnValue=a/b;}", "Main", 0, 127, 0)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { returnValue=a/b;}", "Main", 55, 33, 1)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { returnValue=a/b;}", "Main", 255, 255, 1)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { returnValue=a%b;}", "Main", 128, 127, 1)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { returnValue=a%b;}", "Main", 0, 127, 0)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { returnValue=a%b;}", "Main", 55, 33, 22)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { returnValue=a%b;}", "Main", 255, 255, 0)]
        public void Check8Bit8BitExpects8Bit(string input, string entryPointName, byte ival1, byte ival2, byte expected)
        {
            Assert.True(Input8Bit8BitExpects8BitValue(CompileForTest(input, entryPointName), ival1, ival2, expected), $"Test {entryPointName},{input}");
        }

        [Theory]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { returnValue=a+b;}", "Main", -128, 127, -1)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { returnValue=a+b;}", "Main", -5, -4, -9)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { returnValue=a+b;}", "Main", 55, 33, 88)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { returnValue=a+b;}", "Main", 127, -127, 0)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { returnValue=a-b;}", "Main", -128, 127, 1)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { returnValue=a-b;}", "Main", 0, 127, -127)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { returnValue=a-b;}", "Main", 55, 33, 22)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { returnValue=a-b;}", "Main", 127, -127, -2)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { returnValue=a*b;}", "Main", -2, 64, -128)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { returnValue=a*b;}", "Main", 0, -127, 0)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { returnValue=a*b;}", "Main", 55, -33, -23)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { returnValue=a*b;}", "Main", 127, 127, 1)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { returnValue=a/b;}", "Main", -127, -127, 1)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { returnValue=a/b;}", "Main", 0, -127, 0)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { returnValue=a/b;}", "Main", 55, -33, -1)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { returnValue=a/b;}", "Main", -55, 33, -1)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { returnValue=a/b;}", "Main", 127, 127, 1)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { returnValue=a%b;}", "Main", -127, -127, 0)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { returnValue=a%b;}", "Main", 0, 127, 0)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { returnValue=a%b;}", "Main", -55, -33, -22)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { returnValue=a%b;}", "Main", 127, 127, 0)]
        public void CheckS8BitS8BitExpectsS8Bit(string input, string entryPointName, sbyte ival1, sbyte ival2, sbyte expected)
        {
            Assert.True(InputS8BitS8BitExpectsS8BitValue(CompileForTest(input, entryPointName), ival1, ival2, expected), $"Test {entryPointName},{input},{ival1},{ival2},{expected}");
        }


        [Theory]
        [InlineData(@"Main : (a : *[8]bit) (returnValue : [8]bit) = { returnValue=a[0]; }", "Main", new byte[] { 0, 1 }, 0)]
        [InlineData(@"Main : (a : *[8]bit) (returnValue : [8]bit) = { returnValue=a[1]; }", "Main", new byte[] { 0, 1 }, 1)]
        [InlineData(@"Main : (a : *[8]bit) (returnValue : [8]bit) = { returnValue=a[0]; }", "Main", new byte[] { 15, 23 }, 15)]
        [InlineData(@"Main : (a : *[8]bit) (returnValue : [8]bit) = { returnValue=a[1]; }", "Main", new byte[] { 15, 23 }, 23)]
        [InlineData(@"Main : (a : *[8]bit) (returnValue : [8]bit) = { returnValue=a[1+1+1]; }", "Main", new byte[] { 15, 23, 55, 66 }, 66)]
        public void CheckSubscriptConst(string input, string entryPointName, byte[] inputArray, byte expected)
        {
            Assert.True(InputBytePointerToArrayExpectsByteValue(CompileForTest(input, entryPointName), inputArray, expected), $"Test {entryPointName},{input},{inputArray},{expected}");
        }

        [Theory]
        [InlineData(@"Main : (a : *[8]bit,b : [8]bit) (returnValue : [8]bit) = { returnValue=a[b]; }", "Main", new byte[] { 0, 1 }, 0, 0)]
        [InlineData(@"Main : (a : *[8]bit,b : [8]bit) (returnValue : [8]bit) = { returnValue=a[b]; }", "Main", new byte[] { 0, 1 }, 1, 1)]
        [InlineData(@"Main : (a : *[8]bit,b : [8]bit) (returnValue : [8]bit) = { returnValue=a[b]; }", "Main", new byte[] { 15, 23 }, 0, 15)]
        [InlineData(@"Main : (a : *[8]bit,b : [8]bit) (returnValue : [8]bit) = { returnValue=a[b]; }", "Main", new byte[] { 15, 23 }, 1, 23)]
        [InlineData(@"Main : (a : *[8]bit,b : [8]bit) (returnValue : [8]bit) = { returnValue=a[b-1]; }", "Main", new byte[] { 15, 23 }, 1, 15)]
        [InlineData(@"Main : (a : *[8]bit,b : [8]bit) (returnValue : [8]bit) = { returnValue=a[b--]; }", "Main", new byte[] { 15, 23 }, 1, 23)]
        [InlineData(@"Main : (a : *[8]bit,b : [8]bit) (returnValue : [8]bit) = { returnValue=a[--b]; }", "Main", new byte[] { 15, 23 }, 1, 15)]
        public void CheckSubscriptDynamic(string input, string entryPointName, byte[] inputArray, byte index, byte expected)
        {
            Assert.True(InputBytePointerToArrayByteExpectsByteValue(CompileForTest(input, entryPointName), inputArray, index, expected), $"Test {entryPointName},{input},{inputArray},{index},{expected}");
        }

        [Theory]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { x,y:[8]bit=0; for x=a..b { y=y+1; } returnValue=y;}", "Main", 0, 0, 0)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { x,y:[8]bit=0; for x=a..b { y=y+1; } returnValue=y;}", "Main", 0, 1, 1)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { x,y:[8]bit=0; for x=a..b { y=y+1; } returnValue=y;}", "Main", 0, 99, 99)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { x,y:[8]bit=0; for x=a..b { y=y+1; } returnValue=y;}", "Main", 10, 11, 1)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { x,y:[8]bit=0; for x=a..b { y=y+2; } returnValue=y;}", "Main", 10, 11, 2)]
        public void CheckForIntRangeLoop(string input, string entryPointName, byte ival1, byte ival2, byte expected)
        {
            Assert.True(Input8Bit8BitExpects8BitValue(CompileForTest(input, entryPointName), ival1, ival2, expected), $"Test {entryPointName},{input},{ival1},{ival2},{expected}");
        }

        [Theory]
        [InlineData(@"Main:(a:[8]bit,b:[8]bit)(returnValue:[8]bit)={ x,y,z:[8]bit=0; for x=a..b{for y=a..b{z=z+1;}}returnValue=z;}", "Main", 0, 0, 0)]
        [InlineData(@"Main:(a:[8]bit,b:[8]bit)(returnValue:[8]bit)={ x,y,z:[8]bit=0; for x=a..b{for y=a..b{z=z+1;}}returnValue=z;}", "Main", 0, 1, 1)]
        [InlineData(@"Main:(a:[8]bit,b:[8]bit)(returnValue:[8]bit)={ x,y,z:[8]bit=0; for x=a..b{for y=a..b{z=z+1;}}returnValue=z;}", "Main", 0, 2, 4)]
        [InlineData(@"Main:(a:[8]bit,b:[8]bit)(returnValue:[8]bit)={ x,y,z:[8]bit=0; for x=a..b{for y=a..b{z=z+1;}}returnValue=z;}", "Main", 10, 11, 1)]
        [InlineData(@"Main:(a:[8]bit,b:[8]bit)(returnValue:[8]bit)={ x,y,z:[8]bit=0; for x=a..b{for y=a..b{z=z+1;}}returnValue=z;}", "Main", 10, 12, 4)]
        [InlineData(@"Main:(a:[8]bit,b:[8]bit)(returnValue:[8]bit)={ x,y,z,w:[8]bit=0; for x=a..b{for y=a..b{for z=a..b{w=w+1;}}}returnValue=w;}", "Main", 0, 2, 8)]
        public void CheckNestedForIntRangeLoop(string input, string entryPointName, byte ival1, byte ival2, byte expected)
        {
            Assert.True(Input8Bit8BitExpects8BitValue(CompileForTest(input, entryPointName), ival1, ival2, expected), $"Test {entryPointName},{input},{ival1},{ival2},{expected}");
        }

        [Theory]
        [InlineData(@"Bob:(a:bit,b:bit)(out:bit)={out=a+b;}Main:(a:bit,b:bit)(out:bit)={out=Bob(a,b);}", "Main", 0, 0, 0)]
        [InlineData(@"Bob:(a:bit,b:bit)(out:bit)={out=a+b;}Main:(a:bit,b:bit)(out:bit)={out=Bob(a,b);}", "Main", 0, 1, 1)]
        [InlineData(@"Bob:(a:bit,b:bit)(out:bit)={out=a+b;}Main:(a:bit,b:bit)(out:bit)={out=Bob(a,b);}", "Main", 1, 0, 1)]
        [InlineData(@"Bob:(a:bit,b:bit)(out:bit)={out=a+b;}Main:(a:bit,b:bit)(out:bit)={out=Bob(a,b).out;}", "Main", 0, 0, 0)]
        [InlineData(@"Bob:(a:bit,b:bit)(out:bit)={out=a+b;}Main:(a:bit,b:bit)(out:bit)={out=Bob(a,b).out;}", "Main", 0, 1, 1)]
        [InlineData(@"Bob:(a:bit,b:bit)(out:bit)={out=a+b;}Main:(a:bit,b:bit)(out:bit)={out=Bob(a,b).out;}", "Main", 1, 0, 1)]
        public void CheckSimpleFunctionCall(string input, string entryPointName, byte ival1, byte ival2, byte expected)
        {
            Assert.True(InputBitBitExpectsBitValue(CompileForTest(input, entryPointName), ival1, ival2, expected), $"Test {entryPointName},{input},{ival1},{ival2},{expected}");
        }

        const string MultiReturnCall = @"
Bob:(a:bit,b:bit)(out1:bit,out2:bit)=
{
    out1=b;
    out2=a;
}
Main:(a:bit,b:bit)(out:bit)=
{
    result:=Bob(a,b);
    out=result.out1+result.out2;
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
        [InlineData("Main:()(out:bit)={local:{nest:{a:bit}}=_; local.nest.a=0; out=local.nest.a;}", "Main", 0)]
        [InlineData("Main:()(out:bit)={local:{nest:{a:bit}}=_; local.nest.a=1; out=local.nest.a;}", "Main", 1)]
        [InlineData("Main:()(out:bit)={local:{nest:{deepl:{a:bit}deepr:{a:bit}}}=_; local.nest.deepl.a,local.nest.deepr.a=0; out=local.nest.deepr.a;}", "Main", 0)]
        [InlineData("Main:()(out:bit)={local:{nest:{deepl:{a:bit}deepr:{a:bit}}}=_; local.nest.deepl.a,local.nest.deepr.a=1; out=local.nest.deepr.a;}", "Main", 1)]
        public void CheckAssignNestedStruct(string input, string entryPointName, byte expected)
        {
            Assert.True(InputVoidExpectsBitValue(CompileForTest(input, entryPointName), expected), $"Test {entryPointName},{input},{expected}");
        }

        [Theory]
        [InlineData(@"Main:(a:[8]bit,b:[8]bit)(out:[8]bit)={out=b; if a==99 {out=12;} }", "Main", 99, 22, 12)]
        [InlineData(@"Main:(a:[8]bit,b:[8]bit)(out:[8]bit)={out=b; if a==99 {out=12;} }", "Main", 0, 22, 22)]
        [InlineData(@"Main:(a:[8]bit,b:[8]bit)(out:[8]bit)={out=b; if a!=99 {out=12;} }", "Main", 99, 22, 22)]
        [InlineData(@"Main:(a:[8]bit,b:[8]bit)(out:[8]bit)={out=b; if a!=99 {out=12;} }", "Main", 0, 22, 12)]
        [InlineData(@"Main:(a:[8]bit,b:[8]bit)(out:[8]bit)={out=b; if a<=99 {out=12;} }", "Main", 99, 22, 12)]
        [InlineData(@"Main:(a:[8]bit,b:[8]bit)(out:[8]bit)={out=b; if a<=99 {out=12;} }", "Main", 0, 22, 12)]
        [InlineData(@"Main:(a:[8]bit,b:[8]bit)(out:[8]bit)={out=b; if a<=99 {out=12;} }", "Main", 100, 22, 22)]
        [InlineData(@"Main:(a:[8]bit,b:[8]bit)(out:[8]bit)={out=b; if a<99 {out=12;} }", "Main", 99, 22, 22)]
        [InlineData(@"Main:(a:[8]bit,b:[8]bit)(out:[8]bit)={out=b; if a<99 {out=12;} }", "Main", 0, 22, 12)]
        [InlineData(@"Main:(a:[8]bit,b:[8]bit)(out:[8]bit)={out=b; if a<99 {out=12;} }", "Main", 100, 22, 22)]
        [InlineData(@"Main:(a:[8]bit,b:[8]bit)(out:[8]bit)={out=b; if a>=99 {out=12;} }", "Main", 99, 22, 12)]
        [InlineData(@"Main:(a:[8]bit,b:[8]bit)(out:[8]bit)={out=b; if a>=99 {out=12;} }", "Main", 0, 22, 22)]
        [InlineData(@"Main:(a:[8]bit,b:[8]bit)(out:[8]bit)={out=b; if a>=99 {out=12;} }", "Main", 100, 22, 12)]
        [InlineData(@"Main:(a:[8]bit,b:[8]bit)(out:[8]bit)={out=b; if a>99 {out=12;} }", "Main", 99, 22, 22)]
        [InlineData(@"Main:(a:[8]bit,b:[8]bit)(out:[8]bit)={out=b; if a>99 {out=12;} }", "Main", 0, 22, 22)]
        [InlineData(@"Main:(a:[8]bit,b:[8]bit)(out:[8]bit)={out=b; if a>99 {out=12;} }", "Main", 100, 22, 12)]
        public void CheckIfUnsigned(string input, string entryPointName, byte ival1, byte ival2, byte expected)
        {
            Assert.True(Input8Bit8BitExpects8BitValue(CompileForTest(input, entryPointName), ival1, ival2, expected), $"Test {entryPointName},{input},{ival1},{ival2},{expected}");
        }

        [Theory]
        [InlineData(@"Main:(a:[-8]bit,b:[-8]bit)(out:[-8]bit)={out=b; if a==99 {out=12;} }", "Main", 99, 22, 12)]
        [InlineData(@"Main:(a:[-8]bit,b:[-8]bit)(out:[-8]bit)={out=b; if a==99 {out=12;} }", "Main", -99, 22, 22)]
        [InlineData(@"Main:(a:[-8]bit,b:[-8]bit)(out:[-8]bit)={out=b; if a!=99 {out=12;} }", "Main", 99, 22, 22)]
        [InlineData(@"Main:(a:[-8]bit,b:[-8]bit)(out:[-8]bit)={out=b; if a!=99 {out=12;} }", "Main", -99, 22, 12)]
        [InlineData(@"Main:(a:[-8]bit,b:[-8]bit)(out:[-8]bit)={out=b; if a<=99 {out=12;} }", "Main", 99, 22, 12)]
        [InlineData(@"Main:(a:[-8]bit,b:[-8]bit)(out:[-8]bit)={out=b; if a<=99 {out=12;} }", "Main", -99, 22, 12)]
        [InlineData(@"Main:(a:[-8]bit,b:[-8]bit)(out:[-8]bit)={out=b; if a<=99 {out=12;} }", "Main", 100, 22, 22)]
        [InlineData(@"Main:(a:[-8]bit,b:[-8]bit)(out:[-8]bit)={out=b; if a<99 {out=12;} }", "Main", 99, 22, 22)]
        [InlineData(@"Main:(a:[-8]bit,b:[-8]bit)(out:[-8]bit)={out=b; if a<99 {out=12;} }", "Main", -99, 22, 12)]
        [InlineData(@"Main:(a:[-8]bit,b:[-8]bit)(out:[-8]bit)={out=b; if a<99 {out=12;} }", "Main", 100, 22, 22)]
        [InlineData(@"Main:(a:[-8]bit,b:[-8]bit)(out:[-8]bit)={out=b; if a>=99 {out=12;} }", "Main", 99, 22, 12)]
        [InlineData(@"Main:(a:[-8]bit,b:[-8]bit)(out:[-8]bit)={out=b; if a>=99 {out=12;} }", "Main", -99, 22, 22)]
        [InlineData(@"Main:(a:[-8]bit,b:[-8]bit)(out:[-8]bit)={out=b; if a>=99 {out=12;} }", "Main", 100, 22, 12)]
        [InlineData(@"Main:(a:[-8]bit,b:[-8]bit)(out:[-8]bit)={out=b; if a>99 {out=12;} }", "Main", 99, 22, 22)]
        [InlineData(@"Main:(a:[-8]bit,b:[-8]bit)(out:[-8]bit)={out=b; if a>99 {out=12;} }", "Main", -99, 22, 22)]
        [InlineData(@"Main:(a:[-8]bit,b:[-8]bit)(out:[-8]bit)={out=b; if a>99 {out=12;} }", "Main", 100, 22, 12)]
        public void CheckIfSigned(string input, string entryPointName, sbyte ival1, sbyte ival2, sbyte expected)
        {
            Assert.True(InputS8BitS8BitExpectsS8BitValue(CompileForTest(input, entryPointName), ival1, ival2, expected), $"Test {entryPointName},{input},{ival1},{ival2},{expected}");
        }

        [Theory]
        [InlineData(@"Main:(a:[8]bit,b:[8]bit)(out:[8]bit)={if a==99 {out=12;} else {out=b;}}", "Main", 99, 22, 12)]
        [InlineData(@"Main:(a:[8]bit,b:[8]bit)(out:[8]bit)={if a==99 {out=12;} else {out=b;}}", "Main", 6, 22, 22)]
        public void CheckIfElse(string input, string entryPointName, byte ival1, byte ival2, byte expected)
        {
            Assert.True(Input8Bit8BitExpects8BitValue(CompileForTest(input, entryPointName), ival1, ival2, expected), $"Test {entryPointName},{input},{ival1},{ival2},{expected}");
        }

        [Theory]
        [InlineData(@"ByteEnum:[8]bit{None:=0 All:=255} Main:(a:[8]bit,b:[8]bit)(out:[8]bit)={out=ByteEnum.None;}", "Main", 99, 22, 0)]
        [InlineData(@"ByteEnum:[8]bit{None:=0 All:=255} Main:(a:[8]bit,b:[8]bit)(out:[8]bit)={out=ByteEnum.All;}", "Main", 99, 22, 255)]
        [InlineData(@"ByteEnum:[8]bit{None:=0 All:=255} Main:(a:[8]bit,b:[8]bit)(out:[8]bit)={out=ByteEnum.All&0x1F;}", "Main", 99, 22, 0x1F)]
        [InlineData(@"ByteEnum:[4]bit{None:=0 All:=15} Main:(a:[8]bit,b:[8]bit)(out:[8]bit)={out=ByteEnum.All&3;}", "Main", 99, 22, 3)]
        [InlineData(@"ByteEnum:[8]bit{None:=0 All:=255} EnumToInt:(in:ByteEnum)(out:[8]bit)={out=in&0xFF;} Main:(a:[8]bit,b:[8]bit)(out:[8]bit)={out=EnumToInt(ByteEnum.All);}", "Main", 99, 22, 255)]
        [InlineData(@"ByteEnum:[4]bit{None:=0 All:=15} EnumToInt:(in:ByteEnum)(out:[8]bit)={out=in&3;} Main:(a:[8]bit,b:[8]bit)(out:[8]bit)={out=EnumToInt(ByteEnum.All);}", "Main", 99, 22, 3)]
        [InlineData(@"ByteEnum:[8]bit{None:=0 All:=255} Struct:{a:[8]bit b:[8]bit} EnumToInt:(in:ByteEnum)(out:Struct)={t:Struct=_; t.a=1; t.b=in&0xFF; out=t;} Main:(a:[8]bit,b:[8]bit)(out:[8]bit)={out=EnumToInt(ByteEnum.All).out.b;}", "Main", 99, 22, 255)]
        public void CheckEnumUnsigned(string input, string entryPointName, byte ival1, byte ival2, byte expected)
        {
            Assert.True(Input8Bit8BitExpects8BitValue(CompileForTest(input, entryPointName), ival1, ival2, expected), $"Test {entryPointName},{input},{ival1},{ival2},{expected}");
        }

        [Theory]
        [InlineData(@"Main:(a:[8]bit,b:[8]bit)(out:[8]bit)={out=Add(a,b);} Add:(a:[8]bit,b:[8]bit)(out:[8]bit)={out=a+b;}", "Main", 1,3,4)]
        [InlineData(@"Main:(a:[8]bit,b:[8]bit)(out:[8]bit)={out=a+b+c+d;} c,d:[8]bit=6", "Main", 1,3,16)]
        [InlineData(@"Main:(a:[8]bit,b:[8]bit)(out:[8]bit)={out=a+b+c.V1+c.V2;} c:[8]bit{V1:=5 V2:=10}", "Main", 1,3,19)]
        [InlineData(@"Main:(a:[8]bit,b:[8]bit)(out:[8]bit)={out=a+b+c.V1+c.V2;} c:byte{V1:=5 V2:=10} byte:[8]bit", "Main", 1,3,19)]
        public void CheckUseBeforeDef(string input, string entryPointName, byte ival1, byte ival2, byte expected)
        {
            Assert.True(Input8Bit8BitExpects8BitValue(CompileForTest(input, entryPointName), ival1, ival2, expected), $"Test {entryPointName},{input},{ival1},{ival2},{expected}");
        }
        
        [Theory]
        [InlineData(@"Bool:bit{True:=1 False:=0}Pair:{a:Bool b:Bool}Main:()(out:Bool)={v:Pair=_; v.a=Bool.True; v.b=Bool.False; out=v.a;}", "Main", 1)]
        [InlineData(@"Bool:bit{True:=1 False:=0}Pair:{a:Bool b:Bool}Main:()(out:Bool)={v:Pair=_; v.a=Bool.True; v.b=Bool.False; out=v.a==v.b;}", "Main", 0)]
        public void CheckStructEnum(string input, string entryPointName, byte expected)
        {
            Assert.True(InputVoidExpectsBitValue(CompileForTest(input, entryPointName), expected), $"Test {entryPointName},{input},{expected}");
        }

        [Theory]
        [InlineData(@"value1:[64]bit=99 value2:=99 as *[8]bit Main:()(out:bit)={ out=value1==(value2 as [64]bit);}", "Main", 1)]
        [InlineData(@"PointerAsInt:(in:*[8]bit)(out:[64]bit)={out=in as [64]bit;} Main:()(out:bit)={ val:[64]bit=99; out=PointerAsInt(val as *[8]bit)==val;}", "Main", 1)]
        [InlineData(@"PointerAsInt:(in:*[8]bit)(out:[64]bit)={out=in as [64]bit;} Main:()(out:bit)={ val:[64]bit=99; out=val==PointerAsInt(val as *[8]bit);}", "Main", 1)]
        public void CheckPtrIntCast(string input, string entryPointName, byte expected)
        {
            Assert.True(InputVoidExpectsBitValue(CompileForTest(input, entryPointName), expected), $"Test {entryPointName},{input},{expected}");
        }

        [Theory]
        [InlineData(@"value1:[32]bit=99 value2:[64]bit=99 Main:()(out:bit)={ out=value1==(value2 as [32]bit);}", "Main", 1)]
        public void CheckIntTruncatingCast(string input, string entryPointName, byte expected)
        {
            Assert.True(InputVoidExpectsBitValue(CompileForTest(input, entryPointName), expected), $"Test {entryPointName},{input},{expected}");
        }

        [Theory]
        [InlineData(@"value1:[-64]bit=-99 value2:[32]bit=-99 Main:()(out:bit)={ out=value1==(value2 as [-64]bit);}", "Main", 1)]
        [InlineData(@"value1:[-64]bit=-99 value2:[-32]bit=-99 Main:()(out:bit)={ out=value1==(value2 as [-64]bit);}", "Main", 1)]
        [InlineData(@"value1:[64]bit=-99 value2:[32]bit=-99 Main:()(out:bit)={ out=value1==(value2 as [64]bit);}", "Main", 0)]
        [InlineData(@"value1:[64]bit=-99 value2:[-32]bit=-99 Main:()(out:bit)={ out=value1==(value2 as [64]bit);}", "Main", 0)]
        public void CheckIntExtendingCast(string input, string entryPointName, byte expected)
        {
            Assert.True(InputVoidExpectsBitValue(CompileForTest(input, entryPointName), expected), $"Test {entryPointName},{input},{expected}");
        }

        [Theory]
        [InlineData(@"value1:=99 value2:=99 as *[8]bit Main:()(out:bit)={ out=value1==(value2 as [64]bit);}", "Main", 1)]
        [InlineData(@"value1:=99 value2:=99 as *[8]bit Main:()(out:bit)={ out=(value2 as [64]bit)==value1;}", "Main", 1)]
        public void CheckIntExtension(string input, string entryPointName, byte expected)
        {
            Assert.True(InputVoidExpectsBitValue(CompileForTest(input, entryPointName), expected), $"Test {entryPointName},{input},{expected}");
        }

        [Theory]
        [InlineData(@"value1:=99 Main:()(out:[8]bit)={ Make66(); out=value1; } Make66:()()={ value1=66; }", "Main", 66)]
        [InlineData(@"value1:[8]bit=99 Main:()(out:[8]bit)={ Make(66); out=value1; } Make:(val:[8]bit)()={ value1=val; }", "Main", 66)]
        public void CheckVoidFunctionUsage(string input, string entryPointName, byte expected)
        {
            Assert.True(InputVoidExpects8BitValue(CompileForTest(input, entryPointName), expected), $"Test {entryPointName},{input},{expected}");
        }

        [Theory]
        [InlineData(@"Main:(a:[8]bit,b:[8]bit)(out:[8]bit)={if a!=0 {out=Main(a-1,b)+b;} else {out=b;}}", "Main", 0,3,3)]
        [InlineData(@"Main:(a:[8]bit,b:[8]bit)(out:[8]bit)={if a!=0 {out=Main(a-1,b)+b;} else {out=b;}}", "Main", 1,3,6)]
        [InlineData(@"Main:(a:[8]bit,b:[8]bit)(out:[8]bit)={if a!=0 {out=Main(a-1,b)+b;} else {out=b;}}", "Main", 2,2,6)]
        [InlineData(@"Main:(a:[8]bit,b:[8]bit)(out:[8]bit)={if a!=0 {out=b+Main(a-1,b);} else {out=b;}}", "Main", 2,2,6)]
        [InlineData(@"Main:(a:[8]bit,b:[8]bit)(out:[8]bit)={if a!=0 {out=Main(a-1,b)+Main(a-1,b);} else {out=b;}}", "Main", 2,2,8)]
        public void CheckRecursive(string input, string entryPointName, byte ival1, byte ival2, byte expected)
        {
            Assert.True(Input8Bit8BitExpects8BitValue(CompileForTest(input, entryPointName), ival1, ival2, expected), $"Test {entryPointName},{input},{ival1},{ival2},{expected}");
        }

        [Theory]
        [InlineData(@"Main : (a : *[8]bit) (returnValue : [8]bit) = { returnValue=*a; }", "Main", new byte[] { 21, 31 }, 21)]
        [InlineData(@"Main : (a : *[8]bit) (returnValue : [8]bit) = { a++; returnValue=*a; }", "Main", new byte[] { 21, 31 }, 31)]
        [InlineData(@"Main : (a : *[8]bit) (returnValue : [8]bit) = { returnValue=*a++; }", "Main", new byte[] { 21, 31 }, 21)]
        [InlineData(@"Main : (a : *[8]bit) (returnValue : [8]bit) = { ++a; returnValue=*a; }", "Main", new byte[] { 21, 31 }, 31)]
        [InlineData(@"Main : (a : *[8]bit) (returnValue : [8]bit) = { returnValue=*++a; }", "Main", new byte[] { 21, 31 }, 31)]
        [InlineData(@"Main : (a : *[8]bit) (returnValue : [8]bit) = { ++a; a--; returnValue=*a; }", "Main", new byte[] { 21, 31 }, 21)]
        [InlineData(@"Main : (a : *[8]bit) (returnValue : [8]bit) = { ++a; returnValue=*a--; }", "Main", new byte[] { 21, 31 }, 31)]
        [InlineData(@"Main : (a : *[8]bit) (returnValue : [8]bit) = { ++a; --a; returnValue=*a; }", "Main", new byte[] { 21, 31 }, 21)]
        [InlineData(@"Main : (a : *[8]bit) (returnValue : [8]bit) = { ++a; returnValue=*--a; }", "Main", new byte[] { 21, 31 }, 21)]
        public void CheckDereference(string input, string entryPointName, byte[] inputArray, byte expected)
        {
            Assert.True(InputBytePointerToArrayExpectsByteValue(CompileForTest(input, entryPointName), inputArray, expected), $"Test {entryPointName},{input},{inputArray},{expected}");
        }

        [Theory]
        [InlineData(@"Main : (a : [32]bit) (returnValue : [8]bit) = { ptr:=&a; bytePtr:=ptr as *[8]bit; returnValue=bytePtr[0]; }", "Main", 0xAABBCCDD, 0xDD)]
        [InlineData(@"Main : (a : [32]bit) (returnValue : [8]bit) = { ptr:=&a; bytePtr:=ptr as *[8]bit; returnValue=bytePtr[1]; }", "Main", 0xAABBCCDD, 0xCC)]
        [InlineData(@"Main : (a : [32]bit) (returnValue : [8]bit) = { ptr:=&a; bytePtr:=ptr as *[8]bit; returnValue=bytePtr[2]; }", "Main", 0xAABBCCDD, 0xBB)]
        [InlineData(@"Main : (a : [32]bit) (returnValue : [8]bit) = { ptr:=&a; bytePtr:=ptr as *[8]bit; returnValue=bytePtr[3]; }", "Main", 0xAABBCCDD, 0xAA)]
        public void CheckAddressOf(string input, string entryPointName, uint input32, byte expected)
        {
            Assert.True(Input32BitExpectsByteValue(CompileForTest(input, entryPointName), input32, expected), $"Test {entryPointName},{input},{input32},{expected}");
        }

        [Theory]
        [InlineData(@" FunctionType:()(out:[8]bit) Returns12:FunctionType= { out=12; } Main:(a:[8]bit,b:[8]bit)(out:[8]bit)= { ptrToFunction:FunctionType=Returns12; out = a+b+ptrToFunction(); } ", "Main", 2,3,17)]
        [InlineData(@" FunctionType:()(out:[8]bit) Returns12:FunctionType= { out=12; } Main:(a:[8]bit,b:[8]bit)(out:[8]bit)= { ptrToFunction:FunctionType=_; ptrToFunction=Returns12; out = a+b+ptrToFunction(); } ", "Main", 2,3,17)]
        [InlineData(@" FunctionType:()(out:[8]bit) Returns12:FunctionType= { out=12; } ptrToFunction:FunctionType=_ Main:(a:[8]bit,b:[8]bit)(out:[8]bit)= { ptrToFunction=Returns12; out = a+b+ptrToFunction(); } ", "Main", 2,3,17)]
        [InlineData(@" FunctionType:()(out:[8]bit) Returns12:FunctionType= { out=12; } structFPtr: { ptrToFunction:FunctionType } Main:(a:[8]bit,b:[8]bit)(out:[8]bit)= { t:structFPtr=_; t.ptrToFunction=Returns12; out = a+b+t.ptrToFunction(); } ", "Main", 2,3,17)]
        public void CheckFunctionPointer(string input, string entryPointName, byte ival1, byte ival2, byte expected)
        {
            Assert.True(Input8Bit8BitExpects8BitValue(CompileForTest(input, entryPointName), ival1, ival2, expected), $"Test {entryPointName},{input},{ival1},{ival2},{expected}");
        }

        [Theory]
        [InlineData(@"structure:{a:[8]bit b:[8]bit} Main:()(out:[8]bit)= { t:structure=0; out=t.a+t.b; } ", "Main", 0)]
        [InlineData(@"structA:{b:[8]bit} structB:{a:[8]bit b:structA} Main:()(out:[8]bit)= { t:structB=0; out=t.a+t.b.b; } ", "Main", 0)]
        [InlineData(@" byte:[8]bit array:[10]byte Main:()(out:[8]bit)= { t:array=0; cnt:byte=0; i:byte=_; for i=0..10 { cnt = cnt + t[byte]; } out=cnt; } ", "Main", 0)]
        [InlineData(@"structure:{a:[8]bit b:[8]bit} Main:()(out:[8]bit)= { t:structure=_; t=0; out=t.a+t.b; } ", "Main", 0)]
        [InlineData(@"structA:{b:[8]bit} structB:{a:[8]bit b:structA} Main:()(out:[8]bit)= { t:structB=_; t=0; out=t.a+t.b.b; } ", "Main", 0)]
        [InlineData(@" byte:[8]bit array:[10]byte Main:()(out:[8]bit)= { t:array=_; t=0; cnt:byte=0; i:byte=_; for i=0..10 { cnt = cnt + t[byte]; } out=cnt; } ", "Main", 0)]
        public void CheckZeroInitialiser(string input, string entryPointName, byte expected)
        {
            Assert.True(InputVoidExpects8BitValue(CompileForTest(input, entryPointName), expected), $"Test {entryPointName},{input},{expected}");
        }

        [Theory]
        [InlineData(@"Main : (a : *[8]bit, b:[8]bit) () = { *a=b; }", "Main", new byte[] { 21, 31 }, 12, new byte[] { 12, 31 })]
        [InlineData(@"Main : (a : *[8]bit, b:[8]bit) () = { *a=b; }", "Main", new byte[] { 21, 31 }, 99, new byte[] { 99, 31 })]
        [InlineData(@"Main : (a : *[8]bit, b:[8]bit) () = { a++; *a=b; }", "Main", new byte[] { 21, 31 }, 12, new byte[] { 21, 12 })]
        [InlineData(@"Main : (a : *[8]bit, b:[8]bit) () = { a++; *a=b; }", "Main", new byte[] { 21, 31 }, 99, new byte[] { 21, 99 })]
        [InlineData(@"Main : (a : *[8]bit, b:[8]bit) () = { *a++=b; }", "Main", new byte[] { 21, 31 }, 12, new byte[] { 12, 31 })]
        [InlineData(@"Main : (a : *[8]bit, b:[8]bit) () = { *a++=b; }", "Main", new byte[] { 21, 31 }, 99, new byte[] { 99, 31 })]
        [InlineData(@"Main : (a : *[8]bit, b:[8]bit) () = { ++a; *a=b; }", "Main", new byte[] { 21, 31 }, 12, new byte[] { 21, 12 })]
        [InlineData(@"Main : (a : *[8]bit, b:[8]bit) () = { ++a; *a=b; }", "Main", new byte[] { 21, 31 }, 99, new byte[] { 21, 99 })]
        [InlineData(@"Main : (a : *[8]bit, b:[8]bit) () = { *++a=b; }", "Main", new byte[] { 21, 31 }, 12, new byte[] { 21, 12 })]
        [InlineData(@"Main : (a : *[8]bit, b:[8]bit) () = { *++a=b; }", "Main", new byte[] { 21, 31 }, 99, new byte[] { 21, 99 })]
        [InlineData(@"Main : (a : *[8]bit, b:[8]bit) () = { ++a; a--; *a=b; }", "Main", new byte[] { 21, 31 }, 12, new byte[] { 12, 31 })]
        [InlineData(@"Main : (a : *[8]bit, b:[8]bit) () = { ++a; a--; *a=b; }", "Main", new byte[] { 21, 31 }, 99, new byte[] { 99, 31 })]
        [InlineData(@"Main : (a : *[8]bit, b:[8]bit) () = { ++a; *a--=b; }", "Main", new byte[] { 21, 31 }, 12, new byte[] { 21, 12 })]
        [InlineData(@"Main : (a : *[8]bit, b:[8]bit) () = { ++a; *a--=b; }", "Main", new byte[] { 21, 31 }, 99, new byte[] { 21, 99 })]
        [InlineData(@"Main : (a : *[8]bit, b:[8]bit) () = { ++a; --a; *a=b; }", "Main", new byte[] { 21, 31 }, 12, new byte[] { 12, 31 })]
        [InlineData(@"Main : (a : *[8]bit, b:[8]bit) () = { ++a; --a; *a=b; }", "Main", new byte[] { 21, 31 }, 99, new byte[] { 99, 31 })]
        [InlineData(@"Main : (a : *[8]bit, b:[8]bit) () = { ++a; *--a=b; }", "Main", new byte[] { 21, 31 }, 12, new byte[] { 12, 31 })]
        [InlineData(@"Main : (a : *[8]bit, b:[8]bit) () = { ++a; *--a=b; }", "Main", new byte[] { 21, 31 }, 99, new byte[] { 99, 31 })]
        public void CheckDereferenceStorage(string input, string entryPointName, byte[] inputArray, byte inputB, byte[] expected)
        {
            Assert.True(InputBytePointerToArrayByteExpectsArrayResult(CompileForTest(input, entryPointName), inputArray, inputB, expected), $"Test {entryPointName},{input},{inputArray},{inputB},{expected}");
        }


        [StructLayout(LayoutKind.Explicit)]
        public struct RGBA_CSharp
        {
            [FieldOffset(0)] public byte red;
            [FieldOffset(1)] public byte green;
            [FieldOffset(2)] public byte blue;
            [FieldOffset(3)] public byte alpha;
        }

        const string rgbaTestProgram = @"
U8 : [8]bit

RGBA:
{
    r:U8 
    g:U8 
    b:U8 
    a:U8
} 

FetchRed:(colour:*RGBA)(red:U8)=
{
    red=colour.r;
}

FetchGreen:(colour:*RGBA)(green:U8)=
{
    green=colour.g;
}

FetchBlue:(colour:*RGBA)(blue:U8)=
{
    blue=colour.b;
}

FetchAlpha:(colour:*RGBA)(alpha:U8)=
{
    alpha=colour.a;
}

FetchFirstAlpha:(colour:*RGBA)(alpha:U8)=
{
    alpha=colour[0].a;
}

InsertRed:(colour:*RGBA, red:U8)()=
{
    colour.r=red;
}

InsertGreen:(colour:*RGBA, green:U8)()=
{
    colour.g=green;
}

InsertBlue:(colour:*RGBA, blue:U8)()=
{
    colour.b=blue;
}

InsertAlpha:(colour:*RGBA, alpha:U8)()=
{
    colour.a=alpha;
}

InsertFirstAlpha:(colour:*RGBA, alpha:U8)()=
{
    colour[0].a=alpha;
}
";

        public static IEnumerable<object[]> PointerToStructReadData => new List<object[]>
            {
                new object[] { rgbaTestProgram, "FetchRed", new RGBA_CSharp { red = 44, green = 33, blue = 22, alpha = 11 }, 44 },
                new object[] { rgbaTestProgram, "FetchRed", new RGBA_CSharp { red = 11, green = 22, blue = 33, alpha = 44 }, 11 },
                new object[] { rgbaTestProgram, "FetchGreen", new RGBA_CSharp { red = 44, green = 33, blue = 22, alpha = 11 }, 33 },
                new object[] { rgbaTestProgram, "FetchGreen", new RGBA_CSharp { red = 11, green = 22, blue = 33, alpha = 44 }, 22 },
                new object[] { rgbaTestProgram, "FetchBlue", new RGBA_CSharp { red = 44, green = 33, blue = 22, alpha = 11 }, 22 },
                new object[] { rgbaTestProgram, "FetchBlue", new RGBA_CSharp { red = 11, green = 22, blue = 33, alpha = 44 }, 33 },
                new object[] { rgbaTestProgram, "FetchAlpha", new RGBA_CSharp { red = 44, green = 33, blue = 22, alpha = 11 }, 11 },
                new object[] { rgbaTestProgram, "FetchAlpha", new RGBA_CSharp { red = 11, green = 22, blue = 33, alpha = 44 }, 44 },
                new object[] { rgbaTestProgram, "FetchFirstAlpha", new RGBA_CSharp { red = 44, green = 33, blue = 22, alpha = 11 }, 11 },
                new object[] { rgbaTestProgram, "FetchFirstAlpha", new RGBA_CSharp { red = 11, green = 22, blue = 33, alpha = 44 }, 44 },
            };

        [Theory]
        [MemberData(nameof(PointerToStructReadData))]
        public void CheckPointerToStructureRead(string input, string entryPointName, RGBA_CSharp testStruct, byte expected)
        {
            Assert.True(InputPointerToStructReturns8BitValue<RGBA_CSharp>(CompileForTest(input, entryPointName), testStruct, expected), $"Test {entryPointName},{input},{testStruct},{expected}");
        }

        public static IEnumerable<object[]> PointerToStructWriteData => new List<object[]>
            {
                new object[] { rgbaTestProgram, "InsertRed", new RGBA_CSharp { red = 44, green = 33, blue = 22, alpha = 11 }, 12, new RGBA_CSharp { red = 12, green = 33, blue = 22, alpha = 11 } },
                new object[] { rgbaTestProgram, "InsertRed", new RGBA_CSharp { red = 44, green = 33, blue = 22, alpha = 11 }, 0, new RGBA_CSharp { red = 0, green = 33, blue = 22, alpha = 11 } },
                new object[] { rgbaTestProgram, "InsertRed", new RGBA_CSharp { red = 44, green = 33, blue = 22, alpha = 11 }, 255, new RGBA_CSharp { red = 255, green = 33, blue = 22, alpha = 11 } },
                new object[] { rgbaTestProgram, "InsertGreen", new RGBA_CSharp { red = 44, green = 33, blue = 22, alpha = 11 }, 12, new RGBA_CSharp { red = 44, green = 12, blue = 22, alpha = 11 } },
                new object[] { rgbaTestProgram, "InsertGreen", new RGBA_CSharp { red = 44, green = 33, blue = 22, alpha = 11 }, 0, new RGBA_CSharp { red = 44, green = 0, blue = 22, alpha = 11 } },
                new object[] { rgbaTestProgram, "InsertGreen", new RGBA_CSharp { red = 44, green = 33, blue = 22, alpha = 11 }, 255, new RGBA_CSharp { red = 44, green = 255, blue = 22, alpha = 11 } },
                new object[] { rgbaTestProgram, "InsertBlue", new RGBA_CSharp { red = 44, green = 33, blue = 22, alpha = 11 }, 12, new RGBA_CSharp { red = 44, green = 33, blue = 12, alpha = 11 } },
                new object[] { rgbaTestProgram, "InsertBlue", new RGBA_CSharp { red = 44, green = 33, blue = 22, alpha = 11 }, 0, new RGBA_CSharp { red = 44, green = 33, blue = 0, alpha = 11 } },
                new object[] { rgbaTestProgram, "InsertBlue", new RGBA_CSharp { red = 44, green = 33, blue = 22, alpha = 11 }, 255, new RGBA_CSharp { red = 44, green = 33, blue = 255, alpha = 11 } },
                new object[] { rgbaTestProgram, "InsertAlpha", new RGBA_CSharp { red = 44, green = 33, blue = 22, alpha = 11 }, 12, new RGBA_CSharp { red = 44, green = 33, blue = 22, alpha = 12 } },
                new object[] { rgbaTestProgram, "InsertAlpha", new RGBA_CSharp { red = 44, green = 33, blue = 22, alpha = 11 }, 0, new RGBA_CSharp { red = 44, green = 33, blue = 22, alpha = 0 } },
                new object[] { rgbaTestProgram, "InsertAlpha", new RGBA_CSharp { red = 44, green = 33, blue = 22, alpha = 11 }, 255, new RGBA_CSharp { red = 44, green = 33, blue = 22, alpha = 255 } },
                new object[] { rgbaTestProgram, "InsertFirstAlpha", new RGBA_CSharp { red = 44, green = 33, blue = 22, alpha = 11 }, 12, new RGBA_CSharp { red = 44, green = 33, blue = 22, alpha = 12 } },
                new object[] { rgbaTestProgram, "InsertFirstAlpha", new RGBA_CSharp { red = 44, green = 33, blue = 22, alpha = 11 }, 0, new RGBA_CSharp { red = 44, green = 33, blue = 22, alpha = 0 } },
                new object[] { rgbaTestProgram, "InsertFirstAlpha", new RGBA_CSharp { red = 44, green = 33, blue = 22, alpha = 11 }, 255, new RGBA_CSharp { red = 44, green = 33, blue = 22, alpha = 255 } },
            };

        [Theory]
        [MemberData(nameof(PointerToStructWriteData))]
        public void CheckPointerToStructureWrite(string input, string entryPointName, RGBA_CSharp testStruct, byte insert, RGBA_CSharp expected)
        {
            Assert.True(InputPointerToStructByteReturnsVoid<RGBA_CSharp>(CompileForTest(input, entryPointName), testStruct, insert, expected), $"Test {entryPointName},{input},{testStruct},{insert},{expected}");
        }

        [Theory]
        [InlineData(@"Main : (a : [8]bit) (out : bit) = { out=a[0]; }", "Main", 0b00000000, 0)]
        [InlineData(@"Main : (a : [8]bit) (out : bit) = { out=a[1]; }", "Main", 0b00000000, 0)]
        [InlineData(@"Main : (a : [8]bit) (out : bit) = { out=a[2]; }", "Main", 0b00000000, 0)]
        [InlineData(@"Main : (a : [8]bit) (out : bit) = { out=a[3]; }", "Main", 0b00000000, 0)]
        [InlineData(@"Main : (a : [8]bit) (out : bit) = { out=a[4]; }", "Main", 0b00000000, 0)]
        [InlineData(@"Main : (a : [8]bit) (out : bit) = { out=a[5]; }", "Main", 0b00000000, 0)]
        [InlineData(@"Main : (a : [8]bit) (out : bit) = { out=a[6]; }", "Main", 0b00000000, 0)]
        [InlineData(@"Main : (a : [8]bit) (out : bit) = { out=a[7]; }", "Main", 0b00000000, 0)]
        [InlineData(@"Main : (a : [8]bit) (out : bit) = { out=a[0]; }", "Main", 0b11111111, 1)]
        [InlineData(@"Main : (a : [8]bit) (out : bit) = { out=a[1]; }", "Main", 0b11111111, 1)]
        [InlineData(@"Main : (a : [8]bit) (out : bit) = { out=a[2]; }", "Main", 0b11111111, 1)]
        [InlineData(@"Main : (a : [8]bit) (out : bit) = { out=a[3]; }", "Main", 0b11111111, 1)]
        [InlineData(@"Main : (a : [8]bit) (out : bit) = { out=a[4]; }", "Main", 0b11111111, 1)]
        [InlineData(@"Main : (a : [8]bit) (out : bit) = { out=a[5]; }", "Main", 0b11111111, 1)]
        [InlineData(@"Main : (a : [8]bit) (out : bit) = { out=a[6]; }", "Main", 0b11111111, 1)]
        [InlineData(@"Main : (a : [8]bit) (out : bit) = { out=a[7]; }", "Main", 0b11111111, 1)]
        [InlineData(@"Main : (a : [8]bit) (out : bit) = { out=a[0]; }", "Main", 0b01011001, 1)]
        [InlineData(@"Main : (a : [8]bit) (out : bit) = { out=a[1]; }", "Main", 0b01011001, 0)]
        [InlineData(@"Main : (a : [8]bit) (out : bit) = { out=a[2]; }", "Main", 0b01011001, 0)]
        [InlineData(@"Main : (a : [8]bit) (out : bit) = { out=a[3]; }", "Main", 0b01011001, 1)]
        [InlineData(@"Main : (a : [8]bit) (out : bit) = { out=a[4]; }", "Main", 0b01011001, 1)]
        [InlineData(@"Main : (a : [8]bit) (out : bit) = { out=a[5]; }", "Main", 0b01011001, 0)]
        [InlineData(@"Main : (a : [8]bit) (out : bit) = { out=a[6]; }", "Main", 0b01011001, 1)]
        [InlineData(@"Main : (a : [8]bit) (out : bit) = { out=a[7]; }", "Main", 0b01011001, 0)]
        // Check modulus behaviour
        [InlineData(@"Main : (a : [8]bit) (out : bit) = { out=a[8]; }", "Main", 0b01011001, 1)]
        [InlineData(@"Main : (a : [8]bit) (out : bit) = { out=a[9]; }", "Main", 0b01011001, 0)]
        [InlineData(@"Main : (a : [8]bit) (out : bit) = { out=a[10]; }", "Main", 0b01011001, 0)]
        [InlineData(@"Main : (a : [8]bit) (out : bit) = { out=a[11]; }", "Main", 0b01011001, 1)]
        [InlineData(@"Main : (a : [8]bit) (out : bit) = { out=a[12]; }", "Main", 0b01011001, 1)]
        [InlineData(@"Main : (a : [8]bit) (out : bit) = { out=a[13]; }", "Main", 0b01011001, 0)]
        [InlineData(@"Main : (a : [8]bit) (out : bit) = { out=a[14]; }", "Main", 0b01011001, 1)]
        [InlineData(@"Main : (a : [8]bit) (out : bit) = { out=a[15]; }", "Main", 0b01011001, 0)]
        // Check with negative (-1 to -8 read MSB to LSB respectively)
        [InlineData(@"Main : (a : [8]bit) (out : bit) = { out=a[-1]; }", "Main", 0b01011001, 0)]
        [InlineData(@"Main : (a : [8]bit) (out : bit) = { out=a[-2]; }", "Main", 0b01011001, 1)]
        [InlineData(@"Main : (a : [8]bit) (out : bit) = { out=a[-3]; }", "Main", 0b01011001, 0)]
        [InlineData(@"Main : (a : [8]bit) (out : bit) = { out=a[-4]; }", "Main", 0b01011001, 1)]
        [InlineData(@"Main : (a : [8]bit) (out : bit) = { out=a[-5]; }", "Main", 0b01011001, 1)]
        [InlineData(@"Main : (a : [8]bit) (out : bit) = { out=a[-6]; }", "Main", 0b01011001, 0)]
        [InlineData(@"Main : (a : [8]bit) (out : bit) = { out=a[-7]; }", "Main", 0b01011001, 0)]
        [InlineData(@"Main : (a : [8]bit) (out : bit) = { out=a[-8]; }", "Main", 0b01011001, 1)]
        public void CheckBitExtract(string input, string entryPointName, byte ival, byte expected)
        {
            Assert.True(Input8BitExpectsBitValue(CompileForTest(input, entryPointName), ival, expected), $"Test {entryPointName},{input},{ival},{expected}");
        }

        [Theory]
        [InlineData(@"Main : (a : [8]bit, b:[-8]bit) (out : bit) = { out=a[b]; }", "Main", 0b01011001, 0, 1)]
        [InlineData(@"Main : (a : [8]bit, b:[-8]bit) (out : bit) = { out=a[b]; }", "Main", 0b01011001, 1, 0)]
        [InlineData(@"Main : (a : [8]bit, b:[-8]bit) (out : bit) = { out=a[b]; }", "Main", 0b01011001, 2, 0)]
        [InlineData(@"Main : (a : [8]bit, b:[-8]bit) (out : bit) = { out=a[b]; }", "Main", 0b01011001, 3, 1)]
        [InlineData(@"Main : (a : [8]bit, b:[-8]bit) (out : bit) = { out=a[b]; }", "Main", 0b01011001, 4, 1)]
        [InlineData(@"Main : (a : [8]bit, b:[-8]bit) (out : bit) = { out=a[b]; }", "Main", 0b01011001, 5, 0)]
        [InlineData(@"Main : (a : [8]bit, b:[-8]bit) (out : bit) = { out=a[b]; }", "Main", 0b01011001, 6, 1)]
        [InlineData(@"Main : (a : [8]bit, b:[-8]bit) (out : bit) = { out=a[b]; }", "Main", 0b01011001, 7, 0)]
        // Check modulus behaviour
        [InlineData(@"Main : (a : [8]bit, b:[-8]bit) (out : bit) = { out=a[b]; }", "Main", 0b01011001, 8, 1)]
        [InlineData(@"Main : (a : [8]bit, b:[-8]bit) (out : bit) = { out=a[b]; }", "Main", 0b01011001, 9, 0)]
        [InlineData(@"Main : (a : [8]bit, b:[-8]bit) (out : bit) = { out=a[b]; }", "Main", 0b01011001,10, 0)]
        [InlineData(@"Main : (a : [8]bit, b:[-8]bit) (out : bit) = { out=a[b]; }", "Main", 0b01011001,11, 1)]
        [InlineData(@"Main : (a : [8]bit, b:[-8]bit) (out : bit) = { out=a[b]; }", "Main", 0b01011001,12, 1)]
        [InlineData(@"Main : (a : [8]bit, b:[-8]bit) (out : bit) = { out=a[b]; }", "Main", 0b01011001,13, 0)]
        [InlineData(@"Main : (a : [8]bit, b:[-8]bit) (out : bit) = { out=a[b]; }", "Main", 0b01011001,14, 1)]
        [InlineData(@"Main : (a : [8]bit, b:[-8]bit) (out : bit) = { out=a[b]; }", "Main", 0b01011001,15, 0)]
        // Check with negative (-1 to -8 read MSB to LSB respectively)
        [InlineData(@"Main : (a : [8]bit, b:[-8]bit) (out : bit) = { out=a[b]; }", "Main", 0b01011001,-1, 0)]
        [InlineData(@"Main : (a : [8]bit, b:[-8]bit) (out : bit) = { out=a[b]; }", "Main", 0b01011001,-2, 1)]
        [InlineData(@"Main : (a : [8]bit, b:[-8]bit) (out : bit) = { out=a[b]; }", "Main", 0b01011001,-3, 0)]
        [InlineData(@"Main : (a : [8]bit, b:[-8]bit) (out : bit) = { out=a[b]; }", "Main", 0b01011001,-4, 1)]
        [InlineData(@"Main : (a : [8]bit, b:[-8]bit) (out : bit) = { out=a[b]; }", "Main", 0b01011001,-5, 1)]
        [InlineData(@"Main : (a : [8]bit, b:[-8]bit) (out : bit) = { out=a[b]; }", "Main", 0b01011001,-6, 0)]
        [InlineData(@"Main : (a : [8]bit, b:[-8]bit) (out : bit) = { out=a[b]; }", "Main", 0b01011001,-7, 0)]
        [InlineData(@"Main : (a : [8]bit, b:[-8]bit) (out : bit) = { out=a[b]; }", "Main", 0b01011001,-8, 1)]
        public void CheckBitExtractVariable(string input, string entryPointName, byte ival1, sbyte ival2, byte expected)
        {
            Assert.True(Input8BitS8BitExpectsBitValue(CompileForTest(input, entryPointName), ival1, ival2, expected), $"Test {entryPointName},{input},{ival1},{ival2},{expected}");
        }

        [Theory]
        [InlineData(@"Main : (a:[8]bit)(out:[8]bit)={out=a; out[0]=1;}", "Main", 0, 0b00000001)]
        [InlineData(@"Main : (a:[8]bit)(out:[8]bit)={out=a; out[1]=1;}", "Main", 0, 0b00000010)]
        [InlineData(@"Main : (a:[8]bit)(out:[8]bit)={out=a; out[2]=1;}", "Main", 0, 0b00000100)]
        [InlineData(@"Main : (a:[8]bit)(out:[8]bit)={out=a; out[3]=1;}", "Main", 0, 0b00001000)]
        [InlineData(@"Main : (a:[8]bit)(out:[8]bit)={out=a; out[4]=1;}", "Main", 0, 0b00010000)]
        [InlineData(@"Main : (a:[8]bit)(out:[8]bit)={out=a; out[5]=1;}", "Main", 0, 0b00100000)]
        [InlineData(@"Main : (a:[8]bit)(out:[8]bit)={out=a; out[6]=1;}", "Main", 0, 0b01000000)]
        [InlineData(@"Main : (a:[8]bit)(out:[8]bit)={out=a; out[7]=1;}", "Main", 0, 0b10000000)]
        [InlineData(@"Main : (a:[8]bit)(out:[8]bit)={out=a; out[0]=0;}", "Main", 255, 0b11111110)]
        [InlineData(@"Main : (a:[8]bit)(out:[8]bit)={out=a; out[1]=0;}", "Main", 255, 0b11111101)]
        [InlineData(@"Main : (a:[8]bit)(out:[8]bit)={out=a; out[2]=0;}", "Main", 255, 0b11111011)]
        [InlineData(@"Main : (a:[8]bit)(out:[8]bit)={out=a; out[3]=0;}", "Main", 255, 0b11110111)]
        [InlineData(@"Main : (a:[8]bit)(out:[8]bit)={out=a; out[4]=0;}", "Main", 255, 0b11101111)]
        [InlineData(@"Main : (a:[8]bit)(out:[8]bit)={out=a; out[5]=0;}", "Main", 255, 0b11011111)]
        [InlineData(@"Main : (a:[8]bit)(out:[8]bit)={out=a; out[6]=0;}", "Main", 255, 0b10111111)]
        [InlineData(@"Main : (a:[8]bit)(out:[8]bit)={out=a; out[7]=0;}", "Main", 255, 0b01111111)]
        [InlineData(@"Main : (a:[8]bit)(out:[8]bit)={out=a; out[0]=1;}", "Main", 0b01011001, 0b01011001)]
        [InlineData(@"Main : (a:[8]bit)(out:[8]bit)={out=a; out[1]=1;}", "Main", 0b01011001, 0b01011011)]
        [InlineData(@"Main : (a:[8]bit)(out:[8]bit)={out=a; out[2]=1;}", "Main", 0b01011001, 0b01011101)]
        [InlineData(@"Main : (a:[8]bit)(out:[8]bit)={out=a; out[3]=1;}", "Main", 0b01011001, 0b01011001)]
        [InlineData(@"Main : (a:[8]bit)(out:[8]bit)={out=a; out[4]=1;}", "Main", 0b01011001, 0b01011001)]
        [InlineData(@"Main : (a:[8]bit)(out:[8]bit)={out=a; out[5]=1;}", "Main", 0b01011001, 0b01111001)]
        [InlineData(@"Main : (a:[8]bit)(out:[8]bit)={out=a; out[6]=1;}", "Main", 0b01011001, 0b01011001)]
        [InlineData(@"Main : (a:[8]bit)(out:[8]bit)={out=a; out[7]=1;}", "Main", 0b01011001, 0b11011001)]
        // Check modulus
        [InlineData(@"Main : (a:[8]bit)(out:[8]bit)={out=a; out[8]=1;}", "Main", 0b01011001, 0b01011001)]
        [InlineData(@"Main : (a:[8]bit)(out:[8]bit)={out=a; out[9]=1;}", "Main", 0b01011001, 0b01011011)]
        [InlineData(@"Main : (a:[8]bit)(out:[8]bit)={out=a; out[10]=1;}", "Main", 0b01011001, 0b01011101)]
        [InlineData(@"Main : (a:[8]bit)(out:[8]bit)={out=a; out[11]=1;}", "Main", 0b01011001, 0b01011001)]
        [InlineData(@"Main : (a:[8]bit)(out:[8]bit)={out=a; out[12]=1;}", "Main", 0b01011001, 0b01011001)]
        [InlineData(@"Main : (a:[8]bit)(out:[8]bit)={out=a; out[13]=1;}", "Main", 0b01011001, 0b01111001)]
        [InlineData(@"Main : (a:[8]bit)(out:[8]bit)={out=a; out[14]=1;}", "Main", 0b01011001, 0b01011001)]
        [InlineData(@"Main : (a:[8]bit)(out:[8]bit)={out=a; out[15]=1;}", "Main", 0b01011001, 0b11011001)]
        // Check with negative (-1 to -8 read MSB to LSB respectively)
        [InlineData(@"Main : (a:[8]bit)(out:[8]bit)={out=a; out[-1]=1;}", "Main", 0b01011001, 0b11011001)]
        [InlineData(@"Main : (a:[8]bit)(out:[8]bit)={out=a; out[-2]=1;}", "Main", 0b01011001, 0b01011001)]
        [InlineData(@"Main : (a:[8]bit)(out:[8]bit)={out=a; out[-3]=1;}", "Main", 0b01011001, 0b01111001)]
        [InlineData(@"Main : (a:[8]bit)(out:[8]bit)={out=a; out[-4]=1;}", "Main", 0b01011001, 0b01011001)]
        [InlineData(@"Main : (a:[8]bit)(out:[8]bit)={out=a; out[-5]=1;}", "Main", 0b01011001, 0b01011001)]
        [InlineData(@"Main : (a:[8]bit)(out:[8]bit)={out=a; out[-6]=1;}", "Main", 0b01011001, 0b01011101)]
        [InlineData(@"Main : (a:[8]bit)(out:[8]bit)={out=a; out[-7]=1;}", "Main", 0b01011001, 0b01011011)]
        [InlineData(@"Main : (a:[8]bit)(out:[8]bit)={out=a; out[-8]=1;}", "Main", 0b01011001, 0b01011001)]
        public void CheckBitInsert(string input, string entryPointName, byte ival, byte expected)
        {
            Assert.True(Input8BitExpects8BitValue(CompileForTest(input, entryPointName), ival, expected), $"Test {entryPointName},{input},{ival},{expected}");
        }

        [Theory]
        [InlineData(@"Main : (a:[8]bit,b:[-8]bit,c:bit)(out:[8]bit)={out=a; out[b]=c;}", "Main", 0,0,1, 0b00000001)]
        [InlineData(@"Main : (a:[8]bit,b:[-8]bit,c:bit)(out:[8]bit)={out=a; out[b]=c;}", "Main", 0,1,1, 0b00000010)]
        [InlineData(@"Main : (a:[8]bit,b:[-8]bit,c:bit)(out:[8]bit)={out=a; out[b]=c;}", "Main", 0,2,1, 0b00000100)]
        [InlineData(@"Main : (a:[8]bit,b:[-8]bit,c:bit)(out:[8]bit)={out=a; out[b]=c;}", "Main", 0,3,1, 0b00001000)]
        [InlineData(@"Main : (a:[8]bit,b:[-8]bit,c:bit)(out:[8]bit)={out=a; out[b]=c;}", "Main", 0,4,1, 0b00010000)]
        [InlineData(@"Main : (a:[8]bit,b:[-8]bit,c:bit)(out:[8]bit)={out=a; out[b]=c;}", "Main", 0,5,1, 0b00100000)]
        [InlineData(@"Main : (a:[8]bit,b:[-8]bit,c:bit)(out:[8]bit)={out=a; out[b]=c;}", "Main", 0,6,1, 0b01000000)]
        [InlineData(@"Main : (a:[8]bit,b:[-8]bit,c:bit)(out:[8]bit)={out=a; out[b]=c;}", "Main", 0,7,1, 0b10000000)]
        [InlineData(@"Main : (a:[8]bit,b:[-8]bit,c:bit)(out:[8]bit)={out=a; out[b]=c;}", "Main", 255,0,0, 0b11111110)]
        [InlineData(@"Main : (a:[8]bit,b:[-8]bit,c:bit)(out:[8]bit)={out=a; out[b]=c;}", "Main", 255,1,0, 0b11111101)]
        [InlineData(@"Main : (a:[8]bit,b:[-8]bit,c:bit)(out:[8]bit)={out=a; out[b]=c;}", "Main", 255,2,0, 0b11111011)]
        [InlineData(@"Main : (a:[8]bit,b:[-8]bit,c:bit)(out:[8]bit)={out=a; out[b]=c;}", "Main", 255,3,0, 0b11110111)]
        [InlineData(@"Main : (a:[8]bit,b:[-8]bit,c:bit)(out:[8]bit)={out=a; out[b]=c;}", "Main", 255,4,0, 0b11101111)]
        [InlineData(@"Main : (a:[8]bit,b:[-8]bit,c:bit)(out:[8]bit)={out=a; out[b]=c;}", "Main", 255,5,0, 0b11011111)]
        [InlineData(@"Main : (a:[8]bit,b:[-8]bit,c:bit)(out:[8]bit)={out=a; out[b]=c;}", "Main", 255,6,0, 0b10111111)]
        [InlineData(@"Main : (a:[8]bit,b:[-8]bit,c:bit)(out:[8]bit)={out=a; out[b]=c;}", "Main", 255,7,0, 0b01111111)]
        [InlineData(@"Main : (a:[8]bit,b:[-8]bit,c:bit)(out:[8]bit)={out=a; out[b]=c;}", "Main", 0b01011001,0,1, 0b01011001)]
        [InlineData(@"Main : (a:[8]bit,b:[-8]bit,c:bit)(out:[8]bit)={out=a; out[b]=c;}", "Main", 0b01011001,1,1, 0b01011011)]
        [InlineData(@"Main : (a:[8]bit,b:[-8]bit,c:bit)(out:[8]bit)={out=a; out[b]=c;}", "Main", 0b01011001,2,1, 0b01011101)]
        [InlineData(@"Main : (a:[8]bit,b:[-8]bit,c:bit)(out:[8]bit)={out=a; out[b]=c;}", "Main", 0b01011001,3,1, 0b01011001)]
        [InlineData(@"Main : (a:[8]bit,b:[-8]bit,c:bit)(out:[8]bit)={out=a; out[b]=c;}", "Main", 0b01011001,4,1, 0b01011001)]
        [InlineData(@"Main : (a:[8]bit,b:[-8]bit,c:bit)(out:[8]bit)={out=a; out[b]=c;}", "Main", 0b01011001,5,1, 0b01111001)]
        [InlineData(@"Main : (a:[8]bit,b:[-8]bit,c:bit)(out:[8]bit)={out=a; out[b]=c;}", "Main", 0b01011001,6,1, 0b01011001)]
        [InlineData(@"Main : (a:[8]bit,b:[-8]bit,c:bit)(out:[8]bit)={out=a; out[b]=c;}", "Main", 0b01011001,7,1, 0b11011001)]
        // Check modulusb
        [InlineData(@"Main : (a:[8]bit,b:[-8]bit,c:bit)(out:[8]bit)={out=a; out[b]=c;}", "Main", 0b01011001,8,1, 0b01011001)]
        [InlineData(@"Main : (a:[8]bit,b:[-8]bit,c:bit)(out:[8]bit)={out=a; out[b]=c;}", "Main", 0b01011001,9,1, 0b01011011)]
        [InlineData(@"Main : (a:[8]bit,b:[-8]bit,c:bit)(out:[8]bit)={out=a; out[b]=c;}", "Main", 0b01011001,10,1, 0b01011101)]
        [InlineData(@"Main : (a:[8]bit,b:[-8]bit,c:bit)(out:[8]bit)={out=a; out[b]=c;}", "Main", 0b01011001,11,1, 0b01011001)]
        [InlineData(@"Main : (a:[8]bit,b:[-8]bit,c:bit)(out:[8]bit)={out=a; out[b]=c;}", "Main", 0b01011001,12,1, 0b01011001)]
        [InlineData(@"Main : (a:[8]bit,b:[-8]bit,c:bit)(out:[8]bit)={out=a; out[b]=c;}", "Main", 0b01011001,13,1, 0b01111001)]
        [InlineData(@"Main : (a:[8]bit,b:[-8]bit,c:bit)(out:[8]bit)={out=a; out[b]=c;}", "Main", 0b01011001,14,1, 0b01011001)]
        [InlineData(@"Main : (a:[8]bit,b:[-8]bit,c:bit)(out:[8]bit)={out=a; out[b]=c;}", "Main", 0b01011001,15,1, 0b11011001)]
        // Check with negative (-1 to -8 read MSB to LSB respectbvely)
        [InlineData(@"Main : (a:[8]bit,b:[-8]bit,c:bit)(out:[8]bit)={out=a; out[b]=c;}", "Main", 0b01011001,-1,1, 0b11011001)]
        [InlineData(@"Main : (a:[8]bit,b:[-8]bit,c:bit)(out:[8]bit)={out=a; out[b]=c;}", "Main", 0b01011001,-2,1, 0b01011001)]
        [InlineData(@"Main : (a:[8]bit,b:[-8]bit,c:bit)(out:[8]bit)={out=a; out[b]=c;}", "Main", 0b01011001,-3,1, 0b01111001)]
        [InlineData(@"Main : (a:[8]bit,b:[-8]bit,c:bit)(out:[8]bit)={out=a; out[b]=c;}", "Main", 0b01011001,-4,1, 0b01011001)]
        [InlineData(@"Main : (a:[8]bit,b:[-8]bit,c:bit)(out:[8]bit)={out=a; out[b]=c;}", "Main", 0b01011001,-5,1, 0b01011001)]
        [InlineData(@"Main : (a:[8]bit,b:[-8]bit,c:bit)(out:[8]bit)={out=a; out[b]=c;}", "Main", 0b01011001,-6,1, 0b01011101)]
        [InlineData(@"Main : (a:[8]bit,b:[-8]bit,c:bit)(out:[8]bit)={out=a; out[b]=c;}", "Main", 0b01011001,-7,1, 0b01011011)]
        [InlineData(@"Main : (a:[8]bit,b:[-8]bit,c:bit)(out:[8]bit)={out=a; out[b]=c;}", "Main", 0b01011001,-8,1, 0b01011001)]
        public void CheckBitInsertVariable(string input, string entryPointName, byte ival1, sbyte ival2, byte ival3, byte expected)
        {
            Assert.True(Input8BitS8BitBitExpects8BitValue(CompileForTest(input, entryPointName), ival1, ival2, ival3, expected), $"Test {entryPointName},{input},{ival1},{ival2},{ival3},{expected}");
        }

        [Theory]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[0..0]; }", "Main", 0b11011001, 0b00000001)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[0..1]; }", "Main", 0b11011001, 0b00000001)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[0..2]; }", "Main", 0b11011001, 0b00000001)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[0..3]; }", "Main", 0b11011001, 0b00001001)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[0..4]; }", "Main", 0b11011001, 0b00011001)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[0..5]; }", "Main", 0b11011001, 0b00011001)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[0..6]; }", "Main", 0b11011001, 0b01011001)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[0..7]; }", "Main", 0b11011001, 0b11011001)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[1..0]; }", "Main", 0b11011001, 0b00000001)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[1..1]; }", "Main", 0b11011001, 0b00000000)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[1..2]; }", "Main", 0b11011001, 0b00000000)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[1..3]; }", "Main", 0b11011001, 0b00000100)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[1..4]; }", "Main", 0b11011001, 0b00001100)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[1..5]; }", "Main", 0b11011001, 0b00001100)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[1..6]; }", "Main", 0b11011001, 0b00101100)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[1..7]; }", "Main", 0b11011001, 0b01101100)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[2..0]; }", "Main", 0b11011001, 0b00000001)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[2..1]; }", "Main", 0b11011001, 0b00000000)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[2..2]; }", "Main", 0b11011001, 0b00000000)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[2..3]; }", "Main", 0b11011001, 0b00000010)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[2..4]; }", "Main", 0b11011001, 0b00000110)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[2..5]; }", "Main", 0b11011001, 0b00000110)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[2..6]; }", "Main", 0b11011001, 0b00010110)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[2..7]; }", "Main", 0b11011001, 0b00110110)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[3..0]; }", "Main", 0b11011001, 0b00001001)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[3..1]; }", "Main", 0b11011001, 0b00000100)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[3..2]; }", "Main", 0b11011001, 0b00000010)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[3..3]; }", "Main", 0b11011001, 0b00000001)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[3..4]; }", "Main", 0b11011001, 0b00000011)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[3..5]; }", "Main", 0b11011001, 0b00000011)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[3..6]; }", "Main", 0b11011001, 0b00001011)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[3..7]; }", "Main", 0b11011001, 0b00011011)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[4..0]; }", "Main", 0b11011001, 0b00011001)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[4..1]; }", "Main", 0b11011001, 0b00001100)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[4..2]; }", "Main", 0b11011001, 0b00000110)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[4..3]; }", "Main", 0b11011001, 0b00000011)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[4..4]; }", "Main", 0b11011001, 0b00000001)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[4..5]; }", "Main", 0b11011001, 0b00000001)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[4..6]; }", "Main", 0b11011001, 0b00000101)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[4..7]; }", "Main", 0b11011001, 0b00001101)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[5..0]; }", "Main", 0b11011001, 0b00011001)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[5..1]; }", "Main", 0b11011001, 0b00001100)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[5..2]; }", "Main", 0b11011001, 0b00000110)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[5..3]; }", "Main", 0b11011001, 0b00000011)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[5..4]; }", "Main", 0b11011001, 0b00000001)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[5..5]; }", "Main", 0b11011001, 0b00000000)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[5..6]; }", "Main", 0b11011001, 0b00000010)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[5..7]; }", "Main", 0b11011001, 0b00000110)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[6..0]; }", "Main", 0b11011001, 0b01011001)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[6..1]; }", "Main", 0b11011001, 0b00101100)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[6..2]; }", "Main", 0b11011001, 0b00010110)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[6..3]; }", "Main", 0b11011001, 0b00001011)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[6..4]; }", "Main", 0b11011001, 0b00000101)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[6..5]; }", "Main", 0b11011001, 0b00000010)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[6..6]; }", "Main", 0b11011001, 0b00000001)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[6..7]; }", "Main", 0b11011001, 0b00000011)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[7..0]; }", "Main", 0b11011001, 0b11011001)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[7..1]; }", "Main", 0b11011001, 0b01101100)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[7..2]; }", "Main", 0b11011001, 0b00110110)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[7..3]; }", "Main", 0b11011001, 0b00011011)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[7..4]; }", "Main", 0b11011001, 0b00001101)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[7..5]; }", "Main", 0b11011001, 0b00000110)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[7..6]; }", "Main", 0b11011001, 0b00000011)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[7..7]; }", "Main", 0b11011001, 0b00000001)]
        public void CheckBitExtractRange(string input, string entryPointName, byte ival, byte expected)
        {
            Assert.True(Input8BitExpects8BitValue(CompileForTest(input, entryPointName), ival, expected), $"Test {entryPointName},{input},{ival},{expected}");
        }


        [Theory]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[0..]; }", "Main", 0b11011001, 0b11011001)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[1..]; }", "Main", 0b11011001, 0b11101100)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[2..]; }", "Main", 0b11011001, 0b01110110)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[3..]; }", "Main", 0b11011001, 0b00111011)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[4..]; }", "Main", 0b11011001, 0b10011101)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[5..]; }", "Main", 0b11011001, 0b11001110)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[6..]; }", "Main", 0b11011001, 0b01100111)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[7..]; }", "Main", 0b11011001, 0b10110011)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[8..]; }", "Main", 0b11011001, 0b11011001)]
        public void CheckRotateRight(string input, string entryPointName, byte ival, byte expected)
        {
            Assert.True(Input8BitExpects8BitValue(CompileForTest(input, entryPointName), ival, expected), $"Test {entryPointName},{input},{ival},{expected}");
        }

        [Theory]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[0..]; }", "Main", 0b11011001, 0b11011001)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[-1..]; }", "Main", 0b11011001, 0b10110011)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[-2..]; }", "Main", 0b11011001, 0b01100111)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[-3..]; }", "Main", 0b11011001, 0b11001110)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[-4..]; }", "Main", 0b11011001, 0b10011101)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[-5..]; }", "Main", 0b11011001, 0b00111011)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[-6..]; }", "Main", 0b11011001, 0b01110110)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[-7..]; }", "Main", 0b11011001, 0b11101100)]
        [InlineData(@"Main : (a:[8]bit) (out : [8]bit) = { out=a[-8..]; }", "Main", 0b11011001, 0b11011001)]
        public void CheckRotateLeft(string input, string entryPointName, byte ival, byte expected)
        {
            Assert.True(Input8BitExpects8BitValue(CompileForTest(input, entryPointName), ival, expected), $"Test {entryPointName},{input},{ival},{expected}");
        }

        [Theory]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) (out : [8]bit) = { out=a[b..c]; }", "Main", 0b11011001, 0, 0, 0b00000001)]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) (out : [8]bit) = { out=a[b..c]; }", "Main", 0b11011001, 0, 1, 0b00000001)]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) (out : [8]bit) = { out=a[b..c]; }", "Main", 0b11011001, 0, 2, 0b00000001)]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) (out : [8]bit) = { out=a[b..c]; }", "Main", 0b11011001, 0, 3, 0b00001001)]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) (out : [8]bit) = { out=a[b..c]; }", "Main", 0b11011001, 0, 4, 0b00011001)]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) (out : [8]bit) = { out=a[b..c]; }", "Main", 0b11011001, 0, 5, 0b00011001)]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) (out : [8]bit) = { out=a[b..c]; }", "Main", 0b11011001, 0, 6, 0b01011001)]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) (out : [8]bit) = { out=a[b..c]; }", "Main", 0b11011001, 0, 7, 0b11011001)]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) (out : [8]bit) = { out=a[b..c]; }", "Main", 0b11011001, 1, 0, 0b00000001)]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) (out : [8]bit) = { out=a[b..c]; }", "Main", 0b11011001, 1, 1, 0b00000000)]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) (out : [8]bit) = { out=a[b..c]; }", "Main", 0b11011001, 1, 2, 0b00000000)]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) (out : [8]bit) = { out=a[b..c]; }", "Main", 0b11011001, 1, 3, 0b00000100)]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) (out : [8]bit) = { out=a[b..c]; }", "Main", 0b11011001, 1, 4, 0b00001100)]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) (out : [8]bit) = { out=a[b..c]; }", "Main", 0b11011001, 1, 5, 0b00001100)]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) (out : [8]bit) = { out=a[b..c]; }", "Main", 0b11011001, 1, 6, 0b00101100)]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) (out : [8]bit) = { out=a[b..c]; }", "Main", 0b11011001, 1, 7, 0b01101100)]
        [InlineData(@"Main : (a:[8]bit, b:[8]bit, c:[8]bit) (out : [8]bit) = { out=a[b..c]; }", "Main", 0b11011001, 1, 8, 0b11101100)]
        public void CheckBitExtractRangeVariable(string input, string entryPointName, byte ival1, byte ival2, byte ival3, byte expected)
        {
            Assert.True(Input8Bit8Bit8BitExpects8BitValue(CompileForTest(input, entryPointName), ival1, ival2, ival3, expected), $"Test {entryPointName},{input},{ival1},{ival2},{ival3},{expected}");
        }

        [Theory]
        [InlineData(@"Main : (a:[8]bit,b:[8]bit) (out : [8]bit) = { out=a[b..]; }", "Main", 0b11011001,0, 0b11011001)]
        [InlineData(@"Main : (a:[8]bit,b:[8]bit) (out : [8]bit) = { out=a[b..]; }", "Main", 0b11011001,1, 0b11101100)]
        [InlineData(@"Main : (a:[8]bit,b:[8]bit) (out : [8]bit) = { out=a[b..]; }", "Main", 0b11011001,2, 0b01110110)]
        [InlineData(@"Main : (a:[8]bit,b:[8]bit) (out : [8]bit) = { out=a[b..]; }", "Main", 0b11011001,3, 0b00111011)]
        [InlineData(@"Main : (a:[8]bit,b:[8]bit) (out : [8]bit) = { out=a[b..]; }", "Main", 0b11011001,4, 0b10011101)]
        [InlineData(@"Main : (a:[8]bit,b:[8]bit) (out : [8]bit) = { out=a[b..]; }", "Main", 0b11011001,5, 0b11001110)]
        [InlineData(@"Main : (a:[8]bit,b:[8]bit) (out : [8]bit) = { out=a[b..]; }", "Main", 0b11011001,6, 0b01100111)]
        [InlineData(@"Main : (a:[8]bit,b:[8]bit) (out : [8]bit) = { out=a[b..]; }", "Main", 0b11011001,7, 0b10110011)]
        [InlineData(@"Main : (a:[8]bit,b:[8]bit) (out : [8]bit) = { out=a[b..]; }", "Main", 0b11011001,8, 0b11011001)]
        public void CheckRotateRightVariable(string input, string entryPointName, byte ival1, sbyte ival2, byte expected)
        {
            Assert.True(Input8BitS8BitExpects8BitValue(CompileForTest(input, entryPointName), ival1, ival2, expected), $"Test {entryPointName},{input},{ival1},{ival2},{expected}");
        }

        [Theory]
        [InlineData(@"Main : (a:[8]bit,b:[8]bit) (out : [8]bit) = { out=a[b..]; }", "Main", 0b11011001, 0, 0b11011001)]
        [InlineData(@"Main : (a:[8]bit,b:[8]bit) (out : [8]bit) = { out=a[b..]; }", "Main", 0b11011001,-1, 0b10110011)]
        [InlineData(@"Main : (a:[8]bit,b:[8]bit) (out : [8]bit) = { out=a[b..]; }", "Main", 0b11011001,-2, 0b01100111)]
        [InlineData(@"Main : (a:[8]bit,b:[8]bit) (out : [8]bit) = { out=a[b..]; }", "Main", 0b11011001,-3, 0b11001110)]
        [InlineData(@"Main : (a:[8]bit,b:[8]bit) (out : [8]bit) = { out=a[b..]; }", "Main", 0b11011001,-4, 0b10011101)]
        [InlineData(@"Main : (a:[8]bit,b:[8]bit) (out : [8]bit) = { out=a[b..]; }", "Main", 0b11011001,-5, 0b00111011)]
        [InlineData(@"Main : (a:[8]bit,b:[8]bit) (out : [8]bit) = { out=a[b..]; }", "Main", 0b11011001,-6, 0b01110110)]
        [InlineData(@"Main : (a:[8]bit,b:[8]bit) (out : [8]bit) = { out=a[b..]; }", "Main", 0b11011001,-7, 0b11101100)]
        [InlineData(@"Main : (a:[8]bit,b:[8]bit) (out : [8]bit) = { out=a[b..]; }", "Main", 0b11011001,-8, 0b11011001)]
        public void CheckRotateLeftVariable(string input, string entryPointName, byte ival1, sbyte ival2, byte expected)
        {
            Assert.True(Input8BitS8BitExpects8BitValue(CompileForTest(input, entryPointName), ival1, ival2, expected), $"Test {entryPointName},{input},{ival1},{ival2},{expected}");
        }


        [Theory]
        [InlineData(@" Main : () (returnValue : [8]bit) = { bob:=""Hello World""; returnValue=bob[0]; } ", "Main", (byte)'H')]
        [InlineData(@" global:=""Hello World""\_8 Main : () (returnValue : [8]bit) = { returnValue=global[1]; } ", "Main", (byte)'e')]
        [InlineData(@" Main : () (returnValue : [8]bit) = { bob:=""Hello World""; sue:=bob; returnValue=sue[6]; } ", "Main", (byte)'W')]
        [InlineData(@" Main : () (returnValue : [8]bit) = { bob:=""𐐷""₈; returnValue=bob[0]; } ", "Main", (byte)0xF0)]
        [InlineData(@" Main : () (returnValue : [8]bit) = { bob:=""𐐷""₈; returnValue=bob[1]; } ", "Main", (byte)0x90)]
        [InlineData(@" Main : () (returnValue : [8]bit) = { bob:=""𐐷""₈; returnValue=bob[2]; } ", "Main", (byte)0x90)]
        [InlineData(@" Main : () (returnValue : [8]bit) = { bob:=""𐐷""₈; returnValue=bob[3]; } ", "Main", (byte)0xB7)]
        [InlineData(@" Main : () (returnValue : [8]bit) = { bob:=""𐐷""₈; returnValue=bob[4]; } ", "Main", (byte)0x00)]
        public void CheckArrayString8(string input, string entryPointName, byte expected)
        {
            Assert.True(InputVoidExpects8BitValue(CompileForTest(input, entryPointName), expected), $"Test {entryPointName},{input}");
        }

        [Theory]
        [InlineData(@" Main : () (returnValue : [16]bit) = { bob:=""Hello World""\_16; returnValue=bob[0]; } ", "Main", (ushort)'H')]
        [InlineData(@" global:=""Hello World""\_16 Main : () (returnValue : [16]bit) = { returnValue=global[1]; } ", "Main", (ushort)'e')]
        [InlineData(@" Main : () (returnValue : [16]bit) = { bob:=""Hello World""\_16; sue:=bob; returnValue=sue[6]; } ", "Main", (ushort)'W')]
        [InlineData(@" Main : () (returnValue : [16]bit) = { bob:=""𐐷""₁₆; returnValue=bob[0]; } ", "Main", (ushort)0xD801)]
        [InlineData(@" Main : () (returnValue : [16]bit) = { bob:=""𐐷""₁₆; returnValue=bob[1]; } ", "Main", (ushort)0xDC37)]
        [InlineData(@" Main : () (returnValue : [16]bit) = { bob:=""𐐷""₁₆; returnValue=bob[2]; } ", "Main", (ushort)0)]
        public void CheckArrayString16(string input, string entryPointName, ushort expected)
        {
            Assert.True(InputVoidExpects16BitValue(CompileForTest(input, entryPointName), expected), $"Test {entryPointName},{input}");
        }
        
        [Theory]
        [InlineData(@" Main : () (returnValue : [32]bit) = { bob:=""Hello World""\_32; returnValue=bob[0]; } ", "Main", (uint)'H')]
        [InlineData(@" global:=""Hello World""\_32 Main : () (returnValue : [32]bit) = { returnValue=global[1]; } ", "Main", (uint)'e')]
        [InlineData(@" Main : () (returnValue : [32]bit) = { bob:=""Hello World""\_32; sue:=bob; returnValue=sue[6]; } ", "Main", (uint)'W')]
        [InlineData(@" Main : () (returnValue : [32]bit) = { bob:=""𐐷""₃₂; returnValue=bob[0]; } ", "Main", (uint)0x10437)]
        [InlineData(@" Main : () (returnValue : [32]bit) = { bob:=""𐐷""₃₂; returnValue=bob[1]; } ", "Main", (uint)0)]
        public void CheckArrayString32(string input, string entryPointName, uint expected)
        {
            Assert.True(InputVoidExpects32BitValue(CompileForTest(input, entryPointName), expected), $"Test {entryPointName},{input}");
        }

        public IntPtr CompileForTest(string input, string entryPointName)
        {
            var messages = new CompilerMessages(true, true, false);
            var tokenise = new HumphreyTokeniser(messages);
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens, messages);
            var parsed = parser.File();
            var compiler = new HumphreyCompiler(messages);
            var unit = compiler.Compile(parsed, "test", "x86_64", false, false);

            if (messages.HasErrors)
            {
                throw new Exception($"{messages.Dump()}");
            }

            return unit.JitMethod(entryPointName);
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

        delegate void InputVoidOutput16Bit(ushort* returnVal);

        public static bool InputVoidExpects16BitValue(IntPtr ee, ushort expected)
        {
            var func = Marshal.GetDelegateForFunctionPointer<InputVoidOutput16Bit>(ee);
            ushort returnValue;
            func(&returnValue);
            return returnValue == expected;
        }

        delegate void InputVoidOutput32Bit(uint* returnVal);

        public static bool InputVoidExpects32BitValue(IntPtr ee, uint expected)
        {
            var func = Marshal.GetDelegateForFunctionPointer<InputVoidOutput32Bit>(ee);
            uint returnValue;
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

        delegate void InputBitOutputBitBit(byte inputVal, byte* returnValA, byte* returnValB);

        public static bool InputBitExpectsBitBitValue(IntPtr ee, byte input, byte expectedA, byte expectedB)
        {
            var func = Marshal.GetDelegateForFunctionPointer<InputBitOutputBitBit>(ee);
            byte returnValueA, returnValueB;
            func(input, &returnValueA, &returnValueB);
            return returnValueA == expectedA && returnValueB == expectedB;
        }

        delegate void InputByteOutputByteByte(byte inputVal, byte* returnValA, byte* returnValB);

        public static bool InputByteExpectsByteByteValue(IntPtr ee, byte input, byte expectedA, byte expectedB)
        {
            var func = Marshal.GetDelegateForFunctionPointer<InputBitOutputBitBit>(ee);
            byte returnValueA, returnValueB;
            func(input, &returnValueA, &returnValueB);
            return returnValueA == expectedA && returnValueB == expectedB;
        }


        delegate void InputBitBitOutputBit(byte inputVal1, byte inputVal2, byte* returnVal);

        public static bool InputBitBitExpectsBitValue(IntPtr ee, byte input1, byte input2, byte expected)
        {
            var func = Marshal.GetDelegateForFunctionPointer<InputBitBitOutputBit>(ee);
            byte returnValue;
            func(input1, input2, &returnValue);
            return returnValue == expected;
        }

        delegate void Input8BitOutput8Bit(byte inputVal, byte* returnVal);

        public static bool Input8BitExpects8BitValue(IntPtr ee, byte input, byte expected)
        {
            var func = Marshal.GetDelegateForFunctionPointer<Input8BitOutput8Bit>(ee);
            byte returnValue;
            func(input, &returnValue);
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

        delegate void Input8Bit8Bit8BitOutput8Bit(byte inputVal1, byte inputVal2, byte inputVal3, byte* returnVal);

        public static bool Input8Bit8Bit8BitExpects8BitValue(IntPtr ee, byte input1, byte input2, byte input3, byte expected)
        {
            var func = Marshal.GetDelegateForFunctionPointer<Input8Bit8Bit8BitOutput8Bit>(ee);
            byte returnValue;
            func(input1, input2, input3, &returnValue);
            return returnValue == expected;
        }

        delegate void Input8BitOutputBit(byte inputVal1, byte* returnVal);

        public static bool Input8BitExpectsBitValue(IntPtr ee, byte input, byte expected)
        {
            var func = Marshal.GetDelegateForFunctionPointer<Input8BitOutputBit>(ee);
            byte returnValue;
            func(input, &returnValue);
            return returnValue == expected;
        }

        delegate void Input32BitOutputByte(uint inputVal1, byte* returnVal);

        public static bool Input32BitExpectsByteValue(IntPtr ee, uint input, byte expected)
        {
            var func = Marshal.GetDelegateForFunctionPointer<Input32BitOutputByte>(ee);
            byte returnValue;
            func(input, &returnValue);
            return returnValue == expected;
        }

        delegate void Input8BitS8BitOutputBit(byte inputVal1, sbyte inputVal2, byte* returnVal);

        public static bool Input8BitS8BitExpectsBitValue(IntPtr ee, byte input1, sbyte input2, byte expected)
        {
            var func = Marshal.GetDelegateForFunctionPointer<Input8BitS8BitOutputBit>(ee);
            byte returnValue;
            func(input1, input2, &returnValue);
            return returnValue == expected;
        }

        delegate void Input8BitS8BitOutput8Bit(byte inputVal1, sbyte inputVal2, byte* returnVal);

        public static bool Input8BitS8BitExpects8BitValue(IntPtr ee, byte input1, sbyte input2, byte expected)
        {
            var func = Marshal.GetDelegateForFunctionPointer<Input8BitS8BitOutput8Bit>(ee);
            byte returnValue;
            func(input1, input2, &returnValue);
            return returnValue == expected;
        }

        delegate void Input8BitS8BitBitOutput8Bit(byte inputVal1, sbyte inputVal2, byte inputVal3, byte* returnVal);

        public static bool Input8BitS8BitBitExpects8BitValue(IntPtr ee, byte input1, sbyte input2, byte input3, byte expected)
        {
            var func = Marshal.GetDelegateForFunctionPointer<Input8BitS8BitBitOutput8Bit>(ee);
            byte returnValue;
            func(input1, input2, input3, &returnValue);
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

        delegate void InputPointerToByteArrayByteExpectsArrayResult(byte* inputVal, byte a);

        public static bool InputBytePointerToArrayByteExpectsArrayResult(IntPtr ee, byte[] inputArray, byte input, byte[] expected)
        {
            bool resultMatches = true;
            var func = Marshal.GetDelegateForFunctionPointer<InputPointerToByteArrayByteExpectsArrayResult>(ee);
            fixed (byte* ptr = inputArray)
            {
                func(ptr, input);

                int idx = 0;
                foreach (var expect in expected)
                {
                    resultMatches = resultMatches && (expect == ptr[idx++]);
                }
            }
            return resultMatches;
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
        
        delegate void InputPointerToStructOutput8Bit(void* inputVal, byte* returnVal);

        public static bool InputPointerToStructReturns8BitValue<T>(IntPtr ee, T input, byte expected)
        {
            var unmanagedAddr = Marshal.AllocHGlobal(Marshal.SizeOf(input));
            Marshal.StructureToPtr(input, unmanagedAddr, true);
            var func = Marshal.GetDelegateForFunctionPointer<InputPointerToStructOutput8Bit>(ee);
            byte returnValue;
            func(unmanagedAddr.ToPointer(), &returnValue);
            Marshal.FreeHGlobal(unmanagedAddr);
            return returnValue == expected;
        }
        
        delegate void InputPointerToStructByteOutputVoid(void* inputVal, byte insert);

        public static bool InputPointerToStructByteReturnsVoid<T>(IntPtr ee, T input, byte insert, T expected)
        {
            var unmanagedAddr = Marshal.AllocHGlobal(Marshal.SizeOf(input));
            Marshal.StructureToPtr(input, unmanagedAddr, true);
            var func = Marshal.GetDelegateForFunctionPointer<InputPointerToStructByteOutputVoid>(ee);
            func(unmanagedAddr.ToPointer(), insert);
            T afterFunction=Marshal.PtrToStructure<T>(unmanagedAddr);
            Marshal.FreeHGlobal(unmanagedAddr);
            return afterFunction.Equals(expected);
        }
    }
}

