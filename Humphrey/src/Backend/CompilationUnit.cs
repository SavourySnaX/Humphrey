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

        public CompilationType FetchArrayType(CompilationConstantValue numElements, CompilationType elementType)
        {
            return elementType.AsArray((uint)numElements.Constant);
        }

        public CompilationType FetchIntegerType(CompilationConstantValue numBits)
        {
            bool isSigned = false;
            if (numBits.Constant<BigInteger.Zero)
            {
                numBits.Negate();
                isSigned = true;
            }
            return new CompilationType(contextRef.GetIntType((uint)numBits.Constant), isSigned, false);
        }

        public CompilationType FetchIntegerType(uint numBits, bool isSigned = false)
        {
            return new CompilationType(contextRef.GetIntType(numBits), isSigned, false);
        }

        public CompilationType FetchNamedType(string identifier)
        {
            return symbolTable.FetchType(identifier);
        }

        public void CreateNamedType(string identifier, CompilationType type)
        {
            symbolTable.AddType(identifier, type);
        }

        public CompilationType FetchStructType(CompilationType[] elements)
        {
            var types = new LLVMTypeRef[elements.Length];
            int idx = 0;
            foreach(var element in elements)
                types[idx++] = element.BackendType;

            return new CompilationType(contextRef.GetStructType(types, true), false, false);
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

            // Check for global value
            value = symbolTable.FetchGlobalValue(identifier);
            if (value != null)
                return builder.Load(value);

            // Check for local value
            value = symbolTable.FetchLocalValue(identifier);
            if (value != null)
                return builder.Load(value);

            throw new Exception($"Failed to find identifier {identifier}");
        }

        public CompilationBuilder CreateBuilder(CompilationFunction function, CompilationBlock bb)
        {
            var builder = contextRef.CreateBuilder();
            builder.PositionAtEnd(bb.BackendValue);
            return new CompilationBuilder(builder, function);
        }

        public CompilationValue CreateConstant(CompilationConstantValue constantValue, uint numBits, bool isSigned)
        {
            var constType = new CompilationType(contextRef.GetIntType(numBits), isSigned, false);

            return new CompilationValue(constType.BackendType.CreateConstantValue(constantValue.Constant.ToString(), 10), constType);
        }

        public CompilationValue CreateConstant(AstNumber decimalNumber)
        {
            var constantValue = new CompilationConstantValue(decimalNumber);
            var (numBits, isSigned) = constantValue.ComputeKind();

            return CreateConstant(constantValue, numBits, isSigned);
        }

        public CompilationValue CreateConstant(string decimalNumber)
        {
            return CreateConstant(new AstNumber(decimalNumber));
        }

        public CompilationValue CreateUndef(CompilationType type)
        {
            return new CompilationValue(type.BackendType.Undef, type);
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
                symbolTable.AddFunctionParam(type.Parameters[p].Identifier, cfunc, new CompilationValue(cfunc.BackendValue.Params[p], type.Parameters[p].Type));
            }

            return cfunc;
        }

        public CompilationValue CreateGlobalVariable(CompilationType type, string identifier, CompilationConstantValue initialiser = null)
        {
            if (symbolTable.FetchGlobalValue(identifier)!=null)
                throw new Exception($"global value {identifier} already exists!");

            var global = moduleRef.AddGlobal(type.BackendType, identifier);

            if (initialiser != null)
            {
                var constantValue = initialiser.GetCompilationValue(this, type);
                global.Initializer = constantValue.BackendValue;
            }

            var globalValue = new CompilationValue(global, type);

            if (!symbolTable.AddGlobalValue(identifier, globalValue))
                throw new Exception($"global {identifier} failed to add symbol!");

            return globalValue;
        }

        public CompilationValue CreateLocalVariable(CompilationUnit unit, CompilationBuilder builder, CompilationType type, string identifier, ICompilationValue initialiser)
        {
            if (symbolTable.FetchLocalValue(identifier)!=null)
                throw new Exception($"global value {identifier} already exists!");

            var local = builder.Alloca(type);

            if (initialiser != null)
            {
                CompilationValue value = initialiser as CompilationValue;
                if (initialiser is CompilationConstantValue ccv)
                    value = ccv.GetCompilationValue(unit, type);

                builder.Store(value, local);
            }

            if (!symbolTable.AddLocalValue(identifier, local))
                throw new Exception($"local {identifier} failed to add symbol!");

            return local;
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