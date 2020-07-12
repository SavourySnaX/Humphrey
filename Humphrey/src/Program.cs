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

            BootBoot :              # TODO - this is currently creating a global but shouldn't! it should create a type
            {
                magic           : [32]bit
                size            : [32]bit
                protocol        : [8]bit
                fbType          : [8]bit    # Todo enum
                numCores        : [16]bit
                bootstrapAPICId : [16]bit
                timezone        : [-16]bit
                dateTime        : [64]bit
                initRDPtr       : [64]bit
                initRDSize      : [64]bit
                fbPtr           : [64]bit
                fbSize          : [32]bit
                fbWidth         : [32]bit
                fbHeight        : [32]bit
                fbScanline      : [32]bit
                acpiPtr         : [64]bit
                smbiPtr         : [64]bit
                efiPtr          : [64]bit
                mpPtr           : [64]bit
                unused          : [256]bit  # Would be preferable here to use _ : [256]bit   since we don't need a name
            }

            bootboot    : BootBoot = _

#!
            bootboot    : *BootBoot = (*BootBoot) 0xFFFFFFFFFFE00000
            environment : *[8]bit   = (*[8]bit)   0xFFFFFFFFFFE01000
            framBuffer  : *[8]bit   = (*[8]bit)   0xFFFFFFFFFFC00000

            Main : ()() =
            {
                localBoot = *bootboot;
            }
!#        
        
        ";

        static void LangTest()
        {
            //string test = "initialised : [-8] bit = 0 Main:()(returnValue:[-8]bit)={return initialised;}";
            //string newType = "u8:{_:bit[8];}Main:()(returnValue:u8)={return 51;}";

            var tokeniser = new HumphreyTokeniser();

            var tokens = tokeniser.Tokenize(test);

            var parse = new HumphreyParser(tokens).File();

            var cu = new CompilationUnit("testing");

            foreach (var def in parse)
            {
                def.Compile(cu);
            }

            Console.WriteLine(cu.Dump());
        }

        static void Main(string[] args)
        {
            LangTest();

            var context = CreateContext();
            var module = context.CreateModuleWithName("Hello");

            var paramTypes = new[] { context.Int32Type, context.Int32Type };
            var functionType = CreateFunctionType(context.Int32Type, paramTypes, false);
            var functionSum = module.AddFunction("sum", functionType);

            var bb = functionSum.AppendBasicBlock("entry");

            var builder = context.CreateBuilder();
            builder.PositionAtEnd(bb);
            var addParams = builder.BuildAdd(functionSum.GetParam(0), functionSum.GetParam(1), "AddParameters");
            builder.BuildRet(addParams);

            if (!module.TryVerify(LLVMVerifierFailureAction.LLVMPrintMessageAction, out var message))
            {
                LLVM.DumpModule(module);
                Console.WriteLine($"Module Verification Failed : {message}");
            }

            var options = LLVMMCJITCompilerOptions.Create();
            if (!module.TryCreateMCJITCompiler(out var ee,ref options, out message))
            {
                Console.WriteLine($"Failed to create MCJit : {message}");
            }

            var addMethod = Marshal.GetDelegateForFunctionPointer<Add>(ee.GetPointerToGlobal(functionSum));
            Console.WriteLine(addMethod(5, 2));

            var targetMachine = LLVMTargetRef.First.CreateTargetMachine(LLVMTargetRef.DefaultTriple, "generic", "", LLVMCodeGenOptLevel.LLVMCodeGenLevelAggressive, LLVMRelocMode.LLVMRelocDefault, LLVMCodeModel.LLVMCodeModelDefault);

            module.DataLayout = targetMachine.CreateTargetDataLayout();
            module.Target = LLVMTargetRef.DefaultTriple;

            var pm = LLVMPassManagerRef.Create();
            LLVMPassManagerBuilderRef passes = LLVM.PassManagerBuilderCreate();
            passes.PopulateModulePassManager(pm);
            passes.PopulateFunctionPassManager(pm);

            module.Dump();
            pm.Run(module);
            module.Dump();

            targetMachine.EmitToFile(module, "compiled.o", LLVMCodeGenFileType.LLVMObjectFile);
        }
    }
}
