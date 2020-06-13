using System;
using System.Runtime.InteropServices;
using LLVMSharp.Interop;
using static Extensions.Helpers;

using Humphrey.FrontEnd;
using Superpower.Parsers;
using Superpower;

namespace Humphrey.Experiments
{
    unsafe class Program
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int Add(int a, int b);

        static void LangTest()
        {
            string test = "main";

            var tokeniaser = new HumphreyTokeniser();

            var tokens = tokeniaser.Tokenize(test);

            var parse = HumphreyParser.File.Parse(tokens);
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

            LLVM.LinkInMCJIT();

            LLVM.InitializeX86TargetMC();
            LLVM.InitializeX86Target();
            LLVM.InitializeX86TargetInfo();
            LLVM.InitializeX86AsmParser();
            LLVM.InitializeX86AsmPrinter();

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
