using System;
using System.Runtime.InteropServices;

using Humphrey.FrontEnd;
using Humphrey.Backend;

namespace Humphrey.Experiments
{
    unsafe class Program
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int Add(int a, int b);
            
        static void LangTest()
        {
            var messages = new CompilerMessages(true, true, false);

            var tokeniser = new HumphreyTokeniser(messages);

            var tokens = tokeniser.TokenizeFromFile("../Humphrey.Tests/src/LangTests/001-Basics/100-FirstKernelTests.humphrey");

            var parse = new HumphreyParser(tokens, messages).File();

            if (!messages.HasErrors)
            {
                var cu = new CompilationUnit("testing");

                foreach (var def in parse)
                {
                    def.Compile(cu);
                }

                Console.WriteLine(cu.Dump());

                cu.EmitToFile("compiled.o");
            }

            Console.WriteLine(messages.Dump());
        }

        static void Main(string[] args)
        {
            LangTest();
        }
    }
}
