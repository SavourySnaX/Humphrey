using System;
using System.Runtime.InteropServices;
using LLVMSharp.Interop;
using System.Linq;
using static Extensions.Helpers;

using Humphrey.FrontEnd;
using Humphrey.Backend;

namespace Humphrey.Experiments
{
    unsafe class Program
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int Add(int a, int b);
            
        static readonly string test = @"

            #!
            public enum FrameBufferType : byte
            {
                ARGB=0,
                RGBA=1,
                ABGR=2,
                BGRA=3
            }
            !#

            # identifier : type                 is a type definition
            # identifier : type = value         is a variable definition
            # identifier : type = _             is a variable definition whose value is don't care
            # identifier := expr                is a variable definition whose type reflects the type of the expression
            #
            # identifier = expr                 is an assignment (not a decleration)

            BootBoot :
            {
                magic               : [32]bit
                size                : [32]bit
                protocol            : [8]bit
                fbType              : [8]bit    # Todo enum
                numCores            : [16]bit
                bootstrapAPICId     : [16]bit
                timezone            : [-16]bit
                dateTime            : [64]bit
                initRDPtr           : [64]bit
                initRDSize          : [64]bit
                fbPtr               : [64]bit
                fbSize              : [32]bit
                fbWidth , fbHeight  : [32]bit
                fbScanline          : [32]bit
                acpiPtr             : [64]bit
                smbiPtr             : [64]bit
                efiPtr              : [64]bit
                mpPtr               : [64]bit
                unused              : [256]bit  # Would be preferable here to use _ : [256]bit   since we don't need a name
            }

            bootboot    : *BootBoot = 0xFFFFFFFFFFE00000 as *BootBoot
            environment : *[8]bit   = 0xFFFFFFFFFFE01000 as *[8]bit
            framBuffer  : *[32]bit   = 0xFFFFFFFFFFC00000 as *[32]bit

            Main : ()(result:BootBoot) =
            {
                localBoot := *bootboot
                x,y : [32]bit = 0

                s := localBoot.fbScanline
                w := localBoot.fbWidth
                h := localBoot.fbHeight
#!

                for y in 0..h
                {
                    frameBuffer[s*y+w/2] = 0x00FFFFFF
                }
                for x in 0..w
                {
                    frameBuffer[s*(h/2)+x] = 0x00FFFFFF
                }

                for y*x in 20..40*20..40
                {
                    frameBuffer[s*y+(x+ 0)] = 0x00FF0000
                    frameBuffer[s*y+(x+30)] = 0x0000FF00
                    frameBuffer[s*y+(x+50)] = 0x000000FF
                }
!#
                return localBoot
            }
        
        ";

        static void LangTest()
        {
            var tokeniser = new HumphreyTokeniser();

            var tokens = tokeniser.Tokenize(test);

            var parse = new HumphreyParser(tokens).File();

            var cu = new CompilationUnit("testing");

            foreach (var def in parse)
            {
                def.Compile(cu);
            }

            Console.WriteLine(cu.Dump());
            
            cu.EmitToFile("compiled.o");
        }

        static void Main(string[] args)
        {
            LangTest();
        }
    }
}
