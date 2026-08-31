using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Xunit;

namespace Humphrey.Backend.Tests
{
    /// <summary>
    /// Investigates MCJIT crash: verifies that 500 sequential JIT compilations run without crash.
    /// </summary>
    public unsafe partial class JitTests
    {
        static byte MCJITTestHelper(byte a, byte b)
        {
            return (byte)(a + b);
        }

        static readonly ByteByteToByte s_mcjitDel = new ByteByteToByte(MCJITTestHelper);
        static readonly IntPtr MCJITFuncPtr = Marshal.GetFunctionPointerForDelegate(s_mcjitDel);

        public delegate byte ByteByteToByte(byte a, byte b);

        [Fact]
        public void MCJIT_InvestigateCrashCorrelation()
        {
            // Run 500 tests sequentially, tracking crash point
            int crashAt = -1;
            
            for (int i = 0; i < 500; i++)
            {
                var input = $"[C_CALLING_CONVENTION]TestFunc:(a:[8]bit,b:[8]bit)(out:[8]bit) Main:()(out:[8]bit)={{out=TestFunc({i % 256},({i+1} % 256));}}";
                try
                {
                    IntPtr ee = CompileForTest(input, "Main", new (string name, nint addr)[] { ("TestFunc", MCJITFuncPtr) });
                    var func = Marshal.GetDelegateForFunctionPointer<InputVoidOutput8Bit>(ee);
                    byte res = 0;
                    func(&res);
                }
                catch (Exception ex)
                {
                    crashAt = i;
                    Console.WriteLine($"Crash at iteration {i}: {ex.GetType().Name} - {ex.Message}");
                    break;
                }
            }

            if (crashAt >= 0)
            {
                Assert.Fail($"MCJIT crash at iteration {crashAt} (out of 500)");
            }
            else
            {
                Console.WriteLine("All 500 iterations passed without crash");
            }
        }

        [Fact]
        public void MCJIT_InvestigateCrashWithSingleGlobal()
        {
            for (int run = 0; run < 100; run++)
            {
                var input = $"[C_CALLING_CONVENTION]TestFunc:(a:[8]bit,b:[8]bit)(out:[8]bit) Main:()(out:[8]bit)={{out=TestFunc({run % 256},({run+1} % 256));}}";
                
                var globals = new (string name, nint addr)[] { ("TestFunc", MCJITFuncPtr) };
                
                try
                {
                    IntPtr ee = CompileForTest(input, "Main", globals);
                    var func = Marshal.GetDelegateForFunctionPointer<InputVoidOutput8Bit>(ee);
                    byte res = 0;
                    func(&res);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Crash at run {run}: {ex.Message}");
                    Assert.Fail($"Crash at run {run}: {ex.Message}");
                }
            }
        }
    }
}
