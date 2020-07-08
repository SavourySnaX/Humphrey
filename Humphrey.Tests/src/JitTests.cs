using Xunit;

using Humphrey.FrontEnd;
using System.Runtime.InteropServices;
using System;

namespace Humphrey.Backend.tests
{
    public unsafe class JitTests
    {
        [Theory]
        [InlineData(@" Main : () (returnValue : bit) = { return 0; } ","Main", 0)]
        [InlineData(@" Main : () (returnValue : bit) = { return 1; } ","Main", 1)]
        [InlineData(@" Main : () (returnValue : bit) = { return +1; } ","Main", 1)]
        [InlineData(@" Main : () (returnValue : bit) = { return -1; } ","Main", 1)]
        [InlineData(@" Main : () (returnValue : bit) = { return 1+0; } ","Main", 1)]
        [InlineData(@" Main : () (returnValue : bit) = { return 1-1; } ","Main", 0)]
        [InlineData(@" Main : () (returnValue : bit) = { return 1*1; } ","Main", 1)]
        [InlineData(@" Main : () (returnValue : bit) = { return 1/1; } ","Main", 1)]
        [InlineData(@" Main : () (returnValue : bit) = { return 1%1; } ","Main", 0)]
        [InlineData(@"global : bit = 0 Main : () (returnValue : bit) = { return global; } ","Main", 0)]
        [InlineData(@"global : bit = 1 Main : () (returnValue : bit) = { return global; } ","Main", 1)]
        [InlineData(@"global : bit = +0 Main : () (returnValue : bit) = { return global; } ","Main", 0)]
        [InlineData(@"global : bit = +1 Main : () (returnValue : bit) = { return global; } ","Main", 1)]
        [InlineData(@"global : bit = 0+1 Main : () (returnValue : bit) = { return global; } ","Main", 1)]
        [InlineData(@"global : bit = 0+0 Main : () (returnValue : bit) = { return global; } ","Main", 0)]
        [InlineData(@"global : bit = 1-0 Main : () (returnValue : bit) = { return global; } ","Main", 1)]
        [InlineData(@"global : bit = 1*1 Main : () (returnValue : bit) = { return global; } ","Main", 1)]
        [InlineData(@"global : bit = 1/1 Main : () (returnValue : bit) = { return global; } ","Main", 1)]
        [InlineData(@"global : bit = 1%1 Main : () (returnValue : bit) = { return global; } ","Main", 0)]
        public void CheckVoidExpectsBit(string input, string entryPointName, byte expected)
        {
            Assert.True(InputVoidExpectsBitValue(CompileForTest(input, entryPointName), expected), $"Test {entryPointName},{input}");
        }
        
        [Theory]
        [InlineData(@"Main : (a : bit) (returnValue : bit) = { return a;}","Main", 0, 0)]
        [InlineData(@"Main : (a : bit) (returnValue : bit) = { return a;}","Main", 1, 1)]
        [InlineData(@"Main : (a : bit) (returnValue : bit) = { return +a;}","Main", 0, 0)]
        [InlineData(@"Main : (a : bit) (returnValue : bit) = { return +a;}","Main", 1, 1)]
        [InlineData(@"Main : (a : bit) (returnValue : bit) = { return -a;}","Main", 0, 0)]
        [InlineData(@"Main : (a : bit) (returnValue : bit) = { return -a;}","Main", 1, 1)]
        public void CheckBitExpectsBit(string input, string entryPointName, byte ival, byte expected)
        {
            Assert.True(InputBitExpectsBitValue(CompileForTest(input, entryPointName), ival, expected), $"Test {entryPointName},{input}");
        }

        [Theory]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { return a+b;}","Main", 0, 0, 0)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { return a+b;}","Main", 0, 1, 1)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { return a+b;}","Main", 1, 0, 1)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { return a+b;}","Main", 1, 1, 0)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { return a-b;}","Main", 0, 0, 0)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { return a-b;}","Main", 0, 1, 1)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { return a-b;}","Main", 1, 0, 1)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { return a-b;}","Main", 1, 1, 0)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { return a*b;}","Main", 0, 0, 0)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { return a*b;}","Main", 0, 1, 0)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { return a*b;}","Main", 1, 0, 0)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { return a*b;}","Main", 1, 1, 1)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { return a/b;}","Main", 0, 1, 0)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { return a/b;}","Main", 1, 1, 1)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { return a%b;}","Main", 0, 1, 0)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { return a%b;}","Main", 1, 1, 0)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { return (a+b*1)/1;}","Main", 1, 1, 0)]
        public void CheckBitBitExpectsBit(string input, string entryPointName, byte ival1, byte ival2, byte expected)
        {
            Assert.True(InputBitBitExpectsBitValue(CompileForTest(input, entryPointName), ival1, ival2, expected), $"Test {entryPointName},{input}");
        }

        [Theory]
        [InlineData(@"Main : (a : bit, b : bit) (returnB : bit , returnA : bit) = { return b, a;}","Main", 1, 0, 0, 1)]
        [InlineData(@"Main : (a : bit, b : bit) (returnB : bit , returnA : bit) = { return b, a;}","Main", 0, 1, 1, 0)]
        public void CheckBitBitExpectsBitBit(string input, string entryPointName, byte ival1, byte ival2, byte expected1, byte expected2)
        {
            Assert.True(InputBitBitExpectsBitBitValue(CompileForTest(input, entryPointName), ival1, ival2, expected1, expected2), $"Test {entryPointName},{input}");
        }

        [Theory]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { return a+b;}","Main", 128, 127, 255)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { return a+b;}","Main", 0, 127, 127)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { return a+b;}","Main", 55, 33, 88)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { return a+b;}","Main", 255, 255, 254)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { return a-b;}","Main", 128, 127, 1)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { return a-b;}","Main", 0, 127, 129)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { return a-b;}","Main", 55, 33, 22)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { return a-b;}","Main", 255, 255, 0)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { return a*b;}","Main", 2, 127, 254)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { return a*b;}","Main", 0, 127, 0)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { return a*b;}","Main", 55, 33, 23)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { return a*b;}","Main", 255, 255, 1)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { return a/b;}","Main", 128, 127, 1)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { return a/b;}","Main", 0, 127, 0)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { return a/b;}","Main", 55, 33, 1)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { return a/b;}","Main", 255, 255, 1)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { return a%b;}","Main", 128, 127, 1)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { return a%b;}","Main", 0, 127, 0)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { return a%b;}","Main", 55, 33, 22)]
        [InlineData(@"Main : (a : [8]bit, b : [8]bit) (returnValue : [8]bit) = { return a%b;}","Main", 255, 255, 0)]
        public void Check8Bit8BitExpects8Bit(string input, string entryPointName, byte ival1, byte ival2, byte expected)
        {
            Assert.True(Input8Bit8BitExpects8BitValue(CompileForTest(input, entryPointName), ival1, ival2, expected), $"Test {entryPointName},{input}");
        }

        [Theory]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { return a+b;}","Main", -128, 127, -1)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { return a+b;}","Main", -5, -4, -9)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { return a+b;}","Main", 55, 33, 88)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { return a+b;}","Main", 127, -127, 0)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { return a-b;}","Main", -128, 127, 1)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { return a-b;}","Main", 0, 127, -127)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { return a-b;}","Main", 55, 33, 22)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { return a-b;}","Main", 127, -127, -2)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { return a*b;}","Main", -2, 64, -128)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { return a*b;}","Main", 0, -127, 0)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { return a*b;}","Main", 55, -33, -23)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { return a*b;}","Main", 127, 127, 1)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { return a/b;}","Main", -127, -127, 1)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { return a/b;}","Main", 0, -127, 0)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { return a/b;}","Main", 55, -33, -1)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { return a/b;}","Main", 127, 127, 1)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { return a%b;}","Main", -127, -127, 0)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { return a%b;}","Main", 0, 127, 0)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { return a%b;}","Main", -55, -33, -22)]
        [InlineData(@"Main : (a : [-8]bit, b : [-8]bit) (returnValue : [-8]bit) = { return a%b;}","Main", 127, 127, 0)]
        public void CheckS8BitS8BitExpectsS8Bit(string input, string entryPointName, sbyte ival1, sbyte ival2, sbyte expected)
        {
            Assert.True(InputS8BitS8BitExpectsS8BitValue(CompileForTest(input, entryPointName), ival1, ival2, expected), $"Test {entryPointName},{input},{ival1},{ival2},{expected}");
        }
        public IntPtr CompileForTest(string input, string entryPointName)
        {
            var tokenise = new HumphreyTokeniser();
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens);
            var parsed = parser.File();
            var compiler = new CompilationUnit("test");
            foreach (var def in parsed)
            {
                def.Compile(compiler);
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

