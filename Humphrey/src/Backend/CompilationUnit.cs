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
        Scope symbolScopes;
        LLVMContextRef contextRef;
        LLVMModuleRef moduleRef;

        CompilerMessages messages;

        CompilationDebugBuilder debugBuilder;

        Dictionary<string, IGlobalDefinition> pendingDefinitions;

        Version VersionNumber => new Version(1, 0);
        string CompilerVersion => $"Humphrey Compiler - V{VersionNumber}";

        public CompilationUnit(string sourceFileNameAndPath, IGlobalDefinition[] definitions, CompilerMessages overrideDefaultMessages = null)
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


            var moduleName = System.IO.Path.GetFileNameWithoutExtension(sourceFileNameAndPath);
            contextRef = CreateContext();
            moduleRef = contextRef.CreateModuleWithName(moduleName);
            
            debugBuilder = new CompilationDebugBuilder(this, sourceFileNameAndPath, CompilerVersion, true);

            symbolScopes = new Scope();
            symbolScopes.PushScope(moduleName, debugBuilder.RootScope);

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

            debugBuilder.Finalise();
        }
        public bool CompileMissing(string identifier)
        {
            if (pendingDefinitions.TryGetValue(identifier, out var definition))
            {
                symbolScopes.SaveScopes();
                definition.Compile(this);
                foreach (var ident in definition.Identifiers)
                {
                    pendingDefinitions.Remove(ident.Dump());
                }
                symbolScopes.RestoreScopes();
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

        public (CompilationType compilationType, IType originalType) FetchNamedType(string identifier)
        {
            return symbolScopes.FetchNamedType(identifier);
        }

        public void CreateNamedType(string identifier, CompilationType type, IType originalType)
        {
            var symbTabType = type.CopyAs(identifier);
            symbolScopes.AddType(identifier, symbTabType, originalType);
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
            return symbolScopes.FetchValue(this, identifier, builder);
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
            return symbolScopes.FetchLocation(identifier, builder);
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

        public CompilationFunction CreateFunction(CompilationFunctionType type, AstFunctionType functionType, AstIdentifier identifier)
        {
            string functionName = identifier.Dump();

            if (symbolScopes.FetchFunction(functionName)!=null)
                throw new Exception($"function {functionName} already exists!");

            var func = moduleRef.AddFunction(functionName, type.BackendType);

            var cfunc = new CompilationFunction(func, type);

            if (!symbolScopes.AddFunction(functionName, cfunc))
                throw new Exception($"function {identifier} failed to add symbol!");

            var debugFunction = debugBuilder.CreateDebugFunction(functionName, new SourceLocation(identifier.Token), type,
                new SourceLocation(functionType.Token));

            cfunc.BackendValue.SetSubprogram(debugFunction);

            return cfunc;
        }

        public void PushScope(string identifier, LLVMMetadataRef debugScope)
        {
            symbolScopes.PushScope(identifier, debugScope);
        }

        public void PopScope()
        {
            symbolScopes.PopScope();
        }

        public void AddValue(string identifier, CompilationValue value)
        {
            if (!symbolScopes.AddValue(identifier, value))
            {
                throw new Exception($"TODO duplicate definition error");
            }
        }

        public CompilationValue CreateGlobalVariable(CompilationType type, string identifier, CompilationConstantValue initialiser = null)
        {
            if (symbolScopes.FetchValue(identifier)!=null)
                throw new Exception($"global value {identifier} already exists!");

            var global = moduleRef.AddGlobal(type.BackendType, identifier);

            if (initialiser != null)
            {
                var constantValue = initialiser.GetCompilationValue(this, type);
                global.Initializer = constantValue.BackendValue;
            }

            var globalValue = new CompilationValue(global, type);
            globalValue.Storage = new CompilationValue(global, new CompilationPointerType(CreatePointerType(type.BackendType), type));

            if (!symbolScopes.AddValue(identifier, globalValue))
                throw new Exception($"global {identifier} failed to add symbol!");

            return globalValue;
        }

        public CompilationValue CreateLocalVariable(CompilationUnit unit, CompilationBuilder builder, CompilationType type, string identifier, ICompilationValue initialiser)
        {
            if (symbolScopes.FetchValue(identifier)!=null)
                throw new Exception($"local value {identifier} already exists!");

            var local = builder.Alloca(type);

            if (initialiser != null)
            {
                var value = Expression.ResolveExpressionToValue(unit, initialiser, type);
                builder.Store(value, local);
            }
            local.Storage = new CompilationValue(local.BackendValue, new CompilationPointerType(CreatePointerType(type.BackendType), type));

            if (!symbolScopes.AddValue(identifier, local))
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

            return ee.GetPointerToGlobal(symbolScopes.FetchFunction(identifier).BackendValue);
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

        public LLVMValueRef FetchIntrinsicFunction(string functionName, LLVMTypeRef[] typeRefs)
        {
            return FetchIntrinsic(moduleRef, functionName, typeRefs);
        }

        public LLVMValueRef CreateDebugLocation(SourceLocation location)
        {
            var meta = moduleRef.Context.CreateDebugLocation(location.StartLine, location.StartColumn, symbolScopes.CurrentDebugScope, default(LLVMMetadataRef));
            return meta.AsValue(contextRef);
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

        public void AddModuleFlag(LLVMModuleFlagBehavior behavior, string key, uint value)
        {
            var valueAsConstant = CreateI32Constant(value);

            moduleRef.AddModuleFlag(behavior, key, valueAsConstant.AsMetadata());
        }

        public void AddNamedMetadata(string key, string value)
        {
            moduleRef.AddNamedMetadataWithStringValue(contextRef, key, value);
        }

        public LLVMMetadataRef CreateDebugScope(SourceLocation location)
        {
            return debugBuilder.CreateLexicalScope(symbolScopes.CurrentDebugScope, location);
        }

        public LLVMMetadataRef GetScope(CompilationFunction function)
        {
            return function.BackendValue.GetSubprogram();
        }

        public LLVMModuleRef Module => moduleRef;
        public CompilerMessages Messages => messages;
    }
}