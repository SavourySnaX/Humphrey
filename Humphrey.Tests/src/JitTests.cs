using Xunit;

using Humphrey.FrontEnd;
using System.Runtime.InteropServices;
using System;

namespace Humphrey.Backend.tests
{
    public unsafe class JitTests
    {
        [Theory]
        [InlineData(@"
Main : () (returnValue : bit) =
{
    return 0;
} 
        ","Main", 0)]
        [InlineData(@"
Main : () (returnValue : bit) =
{
    return 1;
} 
        ","Main", 1)]
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
        //[InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { return a*b;}","Main", 0, 0, 0)]       // Multiply causes stack overflow in LLVM to investigate
        //[InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { return a*b;}","Main", 0, 1, 0)]
        //[InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { return a*b;}","Main", 1, 0, 0)]
        //[InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { return a*b;}","Main", 1, 1, 1)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { return a/b;}","Main", 0, 1, 0)]
        [InlineData(@"Main : (a : bit, b : bit) (returnValue : bit) = { return a/b;}","Main", 1, 1, 1)]
        public void CheckBitBitExpectsBit(string input, string entryPointName, byte ival1, byte ival2, byte expected)
        {
            Assert.True(InputBitBitExpectsBitValue(CompileForTest(input, entryPointName), ival1, ival2, expected), $"Test {entryPointName},{input}");
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
    }
}

