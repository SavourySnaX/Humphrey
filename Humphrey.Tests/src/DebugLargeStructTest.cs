using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;
using Humphrey.FrontEnd;
using Humphrey.Compiler.src.Backend;
using Extensions;

namespace Humphrey.Backend.Tests
{
    public partial class JitTests
    {
        [Fact]
        public unsafe void DebugNativeReturnConvention()
        {
            // Test: what does the native function actually return?
            Console.WriteLine("=== Native function direct call ===");
            delegate* unmanaged[Cdecl]<LargeReturnStruct> testDel = &TestABILargeReturnOriginal;
            
            // Get function pointer via MCJIT with a simple test
            var input = @"LargeStruct:{a:[64]bit b:[64]bit c:[64]bit} [C_CALLING_CONVENTION]TestCFunc:()(out:LargeStruct) Main:()(out:[64]bit)={out=TestCFunc().a;}";
            
            var messages = new CompilerMessages(true, true, false);
            var tokenise = new HumphreyTokeniser(messages);
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens, messages);
            var parsed = parser.File();
            var semantic = new SemanticPass(null, messages);
            semantic.RunPass(parsed);
            var compiler = new HumphreyCompiler(messages);
            var currentTarget = Helpers.GetDefaultTargetTriple();
            Console.WriteLine($"Target: {currentTarget}");
            var unit = compiler.Compile(semantic, "test", currentTarget, false, false, false);
            
            Console.WriteLine("=== LLVM IR ===");
            Console.WriteLine(unit.Dump());
            
            // Test 1: Call with ORIGINAL function (returns by value in registers)
            Console.WriteLine("\n=== Test 1: ORIGINAL (by value) ===");
            delegate* unmanaged[Cdecl]<LargeReturnStruct> testDel2 = &TestABILargeReturnOriginal;
            var globals1 = new (string name, nint addr)[] { ("TestCFunc", (nint)testDel2) };
            IntPtr ee1 = unit.JitMethod("Main", globals1);
            
            Console.WriteLine("=== Disassembly (by value) ===");
            Console.WriteLine(unit.FetchDisassembly(false, false));
            
            var func1 = Marshal.GetDelegateForFunctionPointer<InputVoidOutput64Bit>(ee1);
            UInt64 result1 = 0;
            try
            {
                func1(&result1);
                Console.WriteLine($"Result: 0x{result1:X} (expected 0xDEADBEEF)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Crash: {ex.GetType().Name}: {ex.Message}");
            }
        }
        
        [Fact]
        public unsafe void DebugNativeWriteToPtr()
        {
            // Test: write to hidden pointer explicitly
            Console.WriteLine("=== Test: Native writes to hidden pointer ===");
            delegate* unmanaged[Cdecl]<IntPtr, void> testDel = &TestABILargeReturnWithPtr;
            
            var input = @"LargeStruct:{a:[64]bit b:[64]bit c:[64]bit} [C_CALLING_CONVENTION]TestCFunc:()(out:LargeStruct) Main:()(out:[64]bit)={out=TestCFunc().a;}";
            
            var messages = new CompilerMessages(true, true, false);
            var tokenise = new HumphreyTokeniser(messages);
            var tokens = tokenise.Tokenize(input);
            var parser = new HumphreyParser(tokens, messages);
            var parsed = parser.File();
            var semantic = new SemanticPass(null, messages);
            semantic.RunPass(parsed);
            var compiler = new HumphreyCompiler(messages);
            var currentTarget = Helpers.GetDefaultTargetTriple();
            var unit = compiler.Compile(semantic, "test", currentTarget, false, false, false);
            
            var globals = new (string name, nint addr)[] { ("TestCFunc", (nint)testDel) };
            IntPtr ee = unit.JitMethod("Main", globals);
            
            var func = Marshal.GetDelegateForFunctionPointer<InputVoidOutput64Bit>(ee);
            UInt64 result = 0;
            try
            {
                func(&result);
                Console.WriteLine($"Result: 0x{result:X} (expected 0xDEADBEEF)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Crash: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}
