using LLVMSharp.Interop;
using static Extensions.Helpers;

using Humphrey.FrontEnd;
using System.Numerics;
using System;

namespace Humphrey.Backend
{
    public class CompilationUnit
    {
        SymbolTable symbolTable;
        LLVMContextRef contextRef;
        LLVMModuleRef moduleRef;
        public CompilationUnit(string name)
        {
            LLVM.LinkInMCJIT();

            LLVM.InitializeX86TargetMC();
            LLVM.InitializeX86Target();
            LLVM.InitializeX86TargetInfo();
            LLVM.InitializeX86AsmParser();
            LLVM.InitializeX86AsmPrinter();

            symbolTable = new SymbolTable();
            contextRef = CreateContext();
            moduleRef = contextRef.CreateModuleWithName(name);
        }

        public string Dump()
        {
            return moduleRef.PrintToString();
        }

        public CompilationType FetchIntegerType(uint numBits)
        {
            return new CompilationType(contextRef.GetIntType(numBits));
        }

        public CompilationFunctionType CreateFunctionType(CompilationParam[] inputs, CompilationParam[] outputs)
        {
            var allParams = new CompilationParam[inputs.Length + outputs.Length];
            var allBackendParams = new LLVMTypeRef[inputs.Length + outputs.Length];
            var paramIdx = 0;
            foreach(var i in inputs)
            {
                allBackendParams[paramIdx] = i.Type.BackendType;
                allParams[paramIdx++] = i;
            }
            foreach(var o in outputs)
            {
                //outputs need to be considered to be by ref
                allBackendParams[paramIdx] = o.Type.AsPointer().BackendType;
                allParams[paramIdx++] = o;
            }
            return new CompilationFunctionType(Extensions.Helpers.CreateFunctionType(contextRef.VoidType, allBackendParams, false), allParams, (uint)inputs.Length);
        }

        public CompilationValue FetchValue(string identifier, CompilationBuilder builder)
        {
            // Check for function paramter
            var value = symbolTable.FetchFunctionParam(identifier, builder.Function);
            if (value != null)
                return value;

            throw new Exception($"Failed to find identifier {identifier}");
        }

        public CompilationBuilder CreateBuilder(CompilationFunction function, CompilationBlock bb)
        {
            var builder = contextRef.CreateBuilder();
            builder.PositionAtEnd(bb.BackendValue);
            return new CompilationBuilder(builder, function);
        }

        public CompilationValue CreateConstant(string decimalNumber)
        {
            // Compute constant into smallest available type
            var ival = ulong.Parse(decimalNumber);
            uint numBits = 1;
            if (ival!=0)
                numBits = (uint)System.Math.Log(ival, 2) + 1;

            return new CompilationValue(contextRef.GetIntType(numBits).CreateConstantValue(decimalNumber, 10));
        }

        public CompilationFunction CreateFunction(CompilationFunctionType type, string identifier)
        {
            if (symbolTable.FetchFunction(identifier)!=null)
                throw new Exception($"function {identifier} already exists!");

            var func = moduleRef.AddFunction(identifier, type.BackendType);

            var cfunc = new CompilationFunction(func, type);

            if (!symbolTable.AddFunction(identifier, cfunc))
                throw new Exception($"function {identifier} failed to add symbol!");

            for (int p = 0; p < type.Parameters.Length; p++)
            {
                symbolTable.AddFunctionParam(type.Parameters[p].Identifier, cfunc, new CompilationValue(cfunc.BackendValue.Params[p]));
            }

            return cfunc;
        }

        public CompilationParam CreateFunctionParameter(CompilationType type, string identifier)
        {
            return new CompilationParam(type, identifier);
        }

        public delegate bool ExecutionDelegate(IntPtr method);

        public IntPtr JitMethod(string identifier)
        {
            if (!moduleRef.TryVerify(LLVMVerifierFailureAction.LLVMPrintMessageAction, out var message))
            {
                Console.WriteLine($"Module Verification Failed : {message} {moduleRef.PrintToString()}");
            }

            var options = LLVMMCJITCompilerOptions.Create();
            if (!moduleRef.TryCreateMCJITCompiler(out var ee,ref options, out message))
            {
                Console.WriteLine($"Failed to create MCJit : {message}");
            }

            return ee.GetPointerToGlobal(symbolTable.FetchFunction(identifier).BackendValue);
        }
    }
}