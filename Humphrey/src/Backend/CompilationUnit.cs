using LLVMSharp.Interop;
using static Extensions.Helpers;

using Humphrey.FrontEnd;
using System.Numerics;
using System;
using System.IO;
using System.Collections.Generic;

namespace Humphrey.Backend
{
    public class CompilationUnit
    {
        SymbolTable symbolTable;
        LLVMContextRef contextRef;
        LLVMModuleRef moduleRef;

        CompilerMessages messages;

        Dictionary<string, IGlobalDefinition> pendingDefinitions;

        public CompilationUnit(string name, IGlobalDefinition[] definitions, CompilerMessages overrideDefaultMessages = null)
        {
            messages = overrideDefaultMessages;
            if (messages==null)
                messages = new CompilerMessages(true, true, false);

            LLVM.LinkInMCJIT();

            LLVM.InitializeX86TargetMC();
            LLVM.InitializeX86Target();
            LLVM.InitializeX86TargetInfo();
            LLVM.InitializeX86AsmParser();
            LLVM.InitializeX86AsmPrinter();

            symbolTable = new SymbolTable();
            contextRef = CreateContext();
            moduleRef = contextRef.CreateModuleWithName(name);

            pendingDefinitions = new Dictionary<string, IGlobalDefinition>();
            foreach (var def in definitions)
            {
                foreach (var ident in def.Identifiers)
                {
                    pendingDefinitions.Add(ident.Dump(), def);
                }
            }
        }

        public void Compile()
        {
            while (pendingDefinitions.Count!=0)
            {
                var enumerator = pendingDefinitions.Keys.GetEnumerator();
                enumerator.MoveNext();
                CompileMissing(enumerator.Current);
            }
        }
        public bool CompileMissing(string identifier)
        {
            if (pendingDefinitions.TryGetValue(identifier, out var definition))
            {
                definition.Compile(this);
                foreach (var ident in definition.Identifiers)
                {
                    pendingDefinitions.Remove(ident.Dump());
                }
                return true;
            }
            return false;
        }

        public string Dump()
        {
            return moduleRef.PrintToString();
        }

        public CompilationType FetchArrayType(CompilationConstantValue numElements, CompilationType elementType)
        {
            uint num = (uint)numElements.Constant;
            return new CompilationArrayType(CreateArrayType(elementType.BackendType,num), elementType, num);
        }

        public CompilationType FetchIntegerType(CompilationConstantValue numBits)
        {
            bool isSigned = false;
            if (numBits.Constant<BigInteger.Zero)
            {
                numBits.Negate();
                isSigned = true;
            }
            var num = (uint)numBits.Constant;
            return new CompilationIntegerType(contextRef.GetIntType(num), isSigned);
        }

        public CompilationType FetchIntegerType(uint numBits, bool isSigned = false)
        {
            return new CompilationIntegerType(contextRef.GetIntType(numBits), isSigned);
        }

        public CompilationType FetchNamedType(string identifier)
        {
            return symbolTable.FetchType(identifier);
        }

        public void CreateNamedType(string identifier, CompilationType type)
        {
            var symbTabType = type.CopyAs(identifier);
            symbolTable.AddType(identifier, symbTabType);
        }

        public CompilationType FetchStructType(CompilationType[] elements, string[] names)
        {
            var types = new LLVMTypeRef[elements.Length];
            int idx = 0;
            foreach(var element in elements)
                types[idx++] = element.BackendType;

            return new CompilationStructureType(contextRef.GetStructType(types, true), elements, names);
        }

        public CompilationType FetchEnumType(CompilationType type, CompilationConstantValue[] values, Dictionary<string,uint> names)
        {
            return new CompilationEnumType(type, values ,names);
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
                allBackendParams[paramIdx] = CreatePointerType(o.Type.BackendType);
                allParams[paramIdx++] = o;
            }
            return new CompilationFunctionType(Extensions.Helpers.CreateFunctionType(contextRef.VoidType, allBackendParams, false), allParams, (uint)inputs.Length);
        }

        public CompilationValue FetchValueInternal(string identifier, CompilationBuilder builder)
        {
            // Check for function paramter
            var value = symbolTable.FetchFunctionInputParam(identifier, builder.Function);
            if (value != null)
                return value;

            // Check for function paramter
            value = symbolTable.FetchFunctionOutputParam(identifier, builder.Function);
            if (value != null)
                return builder.Load(value);

            // Check for global value
            value = symbolTable.FetchGlobalValue(identifier);
            if (value != null)
                return builder.Load(value);

            // Check for local value
            value = symbolTable.FetchLocalValue(identifier);
            if (value != null)
                return builder.Load(value);

            // Check for function - i guess we can construct this on the fly?
            var function = symbolTable.FetchFunction(identifier);
            if (function!=null)
            {
                value = new CompilationValue(function.BackendValue, function.FunctionType);
                return value;
            }

            // Check for named type (an enum is actually a value type) - might be better done at definition actually
            var nType = symbolTable.FetchType(identifier);
            if (nType!=null)
            {
                value = CreateUndef(nType);
                return value;
            }

            return null;
        }

        public CompilationValue FetchValue(string identifier, CompilationBuilder builder)
        {
            var resolved = FetchValueInternal(identifier, builder);
            if (resolved==null)
            {
                if (CompileMissing(identifier))
                {
                    resolved = FetchValueInternal(identifier, builder);
                }
            }
            if (resolved == null)
                throw new Exception($"Failed to find identifier {identifier}");

            return resolved;
        }

        private CompilationValue FetchLocationInternal(string identifier, CompilationBuilder builder)
        {
            // Check for global value
            var value = symbolTable.FetchGlobalValue(identifier);
            if (value != null)
                return value.Storage;
                            
            // Check for local value
            value = symbolTable.FetchLocalValue(identifier);
            if (value != null)
                return value.Storage;

            // Check for function output param
            value = symbolTable.FetchFunctionOutputParam(identifier, builder.Function);
            if (value != null)
            {
                builder.Function.MarkUsed(identifier);
                return value;
            }
            return null;
        }

        public CompilationValue FetchLocation(string identifier, CompilationBuilder builder)
        {
            var resolved = FetchLocationInternal(identifier, builder);
            if (resolved==null)
            {
                if (CompileMissing(identifier))
                {
                    resolved = FetchLocationInternal(identifier, builder);
                }
            }
            if (resolved == null)
                throw new Exception($"Failed to find identifier {identifier}");

            return resolved;
        }

        public CompilationBuilder CreateBuilder(CompilationFunction function, CompilationBlock bb)
        {
            var builder = contextRef.CreateBuilder();
            builder.PositionAtEnd(bb.BackendValue);
            return new CompilationBuilder(builder, function, bb);
        }

        public LLVMValueRef CreateI32Constant(UInt32 value)
        {
            var i32Type = contextRef.GetIntType(32);
            return i32Type.CreateConstantValue(value);
        }

        public LLVMValueRef CreateI64Constant(UInt64 value)
        {
            var i64Type = contextRef.GetIntType(64);
            return i64Type.CreateConstantValue(value);
        }

        public CompilationValue CreateConstant(CompilationConstantValue constantValue, uint numBits, bool isSigned)
        {
            var constType = new CompilationIntegerType(contextRef.GetIntType(numBits), isSigned);

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

            for (uint p = 0; p < type.OutParamOffset && p < type.Parameters.Length; p++)
            {
                symbolTable.AddFunctionInputParam(type.Parameters[p].Identifier, cfunc, new CompilationValue(cfunc.BackendValue.Params[p], type.Parameters[p].Type));
            }

            for (uint p = type.OutParamOffset; p < type.Parameters.Length; p++)
            {
                symbolTable.AddFunctionOutputParam(type.Parameters[p].Identifier, cfunc, new CompilationValue(cfunc.BackendValue.Params[p], new CompilationPointerType(Extensions.Helpers.CreatePointerType(type.Parameters[p].Type.BackendType), type.Parameters[p].Type)));
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
            globalValue.Storage = new CompilationValue(global, new CompilationPointerType(CreatePointerType(type.BackendType), type));

            if (!symbolTable.AddGlobalValue(identifier, globalValue))
                throw new Exception($"global {identifier} failed to add symbol!");

            return globalValue;
        }

        public CompilationValue CreateLocalVariable(CompilationUnit unit, CompilationBuilder builder, CompilationType type, string identifier, ICompilationValue initialiser)
        {
            if (symbolTable.FetchLocalValue(identifier)!=null)
                throw new Exception($"local value {identifier} already exists!");

            var local = builder.Alloca(type);

            if (initialiser != null)
            {
                var value = Expression.ResolveExpressionToValue(unit, initialiser, type);
                builder.Store(value, local);
            }
            local.Storage = new CompilationValue(local.BackendValue, new CompilationPointerType(CreatePointerType(type.BackendType), type));

            if (!symbolTable.AddLocalValue(identifier, local))
                throw new Exception($"local {identifier} failed to add symbol!");

            return local;
        }
        public CompilationParam CreateFunctionParameter(CompilationType type, AstIdentifier identifier)
        {
            return new CompilationParam(type, identifier);
        }

        public delegate bool ExecutionDelegate(IntPtr method);

        public IntPtr JitMethod(string identifier)
        {
            if (!moduleRef.TryVerify(LLVMVerifierFailureAction.LLVMPrintMessageAction, out var message))
            {
                Console.WriteLine($"Module Verification Failed : {message} {moduleRef.PrintToString()}");
                throw new System.Exception($"Failed to compile module");
            }

            var options = LLVMMCJITCompilerOptions.Create();
            if (!moduleRef.TryCreateMCJITCompiler(out var ee,ref options, out message))
            {
                Console.WriteLine($"Failed to create MCJit : {message}");
                throw new System.Exception($"Failed to create jit");
            }

            return ee.GetPointerToGlobal(symbolTable.FetchFunction(identifier).BackendValue);
        }

        public bool EmitToBitCodeFile(string filename)
        {
            moduleRef.WriteBitcodeToFile(filename);
            return true;
        }

        public bool EmitToFile(string filename, string targetTriple)
        {
            var targetMachine = LLVMTargetRef.First.CreateTargetMachine(targetTriple, "generic", "", LLVMCodeGenOptLevel.LLVMCodeGenLevelAggressive, LLVMRelocMode.LLVMRelocDefault, LLVMCodeModel.LLVMCodeModelDefault);

            moduleRef.DataLayout = targetMachine.CreateTargetDataLayout();
            moduleRef.Target = LLVMTargetRef.DefaultTriple;

            var pm = LLVMPassManagerRef.Create();
            var passes = PassManagerBuilderCreate();
            passes.PopulateModulePassManager(pm);
            passes.PopulateFunctionPassManager(pm);

            if (!moduleRef.TryVerify(LLVMVerifierFailureAction.LLVMPrintMessageAction, out var message))
            {
                messages.Log(CompilerErrorKind.Error_FailedVerification, $"Module Verification Failed : {moduleRef.PrintToString()}{Environment.NewLine}{message}");
                return false;
            }

            pm.Run(moduleRef);

            targetMachine.EmitToFile(moduleRef, filename, LLVMCodeGenFileType.LLVMObjectFile);

            return true;
        }

        public bool DumpDisassembly(string targetTriple)
        {
            var targetMachine = LLVMTargetRef.First.CreateTargetMachine(targetTriple, "generic", "", LLVMCodeGenOptLevel.LLVMCodeGenLevelAggressive, LLVMRelocMode.LLVMRelocDefault, LLVMCodeModel.LLVMCodeModelDefault);

            moduleRef.DataLayout = targetMachine.CreateTargetDataLayout();
            moduleRef.Target = LLVMTargetRef.DefaultTriple;

            var pm = LLVMPassManagerRef.Create();
            var passes = PassManagerBuilderCreate();
            passes.PopulateModulePassManager(pm);
            passes.PopulateFunctionPassManager(pm);

            if (!moduleRef.TryVerify(LLVMVerifierFailureAction.LLVMPrintMessageAction, out var message))
            {
                messages.Log(CompilerErrorKind.Error_FailedVerification, $"Module Verification Failed : {moduleRef.PrintToString()}{Environment.NewLine}{message}");
                return false;
            }

            pm.Run(moduleRef);

            string tmp = Path.GetTempFileName();

            targetMachine.EmitToFile(moduleRef, tmp, LLVMCodeGenFileType.LLVMAssemblyFile);

            Console.WriteLine(File.ReadAllText(tmp));

            File.Delete(tmp);

            return true;
        }

        public CompilerMessages Messages => messages;
    }
}