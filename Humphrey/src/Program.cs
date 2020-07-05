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

        static void LangTest()
        {
            string test = "initialised : [8] bit = -128 Main:()(returnValue:[8]bit)={return initialised;}";
            //string newType = "u8:{_:bit[8];}Main:()(returnValue:u8)={return 51;}";

            var tokeniser = new HumphreyTokeniser();

            var tokens = tokeniser.Tokenize(test).ToList();

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
