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
        string targetTriple;

        IPackageManager packageManager;
        CommonSymbolTable root;
        CommonSymbolTable currentNamespace;
        CommonSymbolTable currentScope;
        Stack<LLVMMetadataRef> debugScopeStack;
        LLVMMetadataRef rootDebugScope;

        LLVMContextRef contextRef;
        LLVMModuleRef moduleRef;
        LLVMTargetDataRef targetDataRef;

        CompilerMessages messages;

        CompilationDebugBuilder debugBuilder;

        Version VersionNumber => new Version(1, 0);
        string CompilerVersion => $"Humphrey Compiler - V{VersionNumber}";

        bool optimisations;

        public CompilationUnit(string sourceFileNameAndPath, CommonSymbolTable rootFromSemmantic, IEnumerable<SemanticPass.SymbolTableAndPass> extraNamespaces, IPackageManager manager, IEnumerable<IGlobalDefinition> definitions, string targetTriple, bool disableOptimisations, bool debugInfo, CompilerMessages overrideDefaultMessages = null)
        {
            optimisations = !disableOptimisations;

            this.targetTriple = targetTriple;

            messages = overrideDefaultMessages;
            if (messages==null)
                messages = new CompilerMessages(true, true, false);

            LLVM.LinkInMCJIT();

            LLVM.InitializeX86TargetMC();
            LLVM.InitializeX86Target();
            LLVM.InitializeX86TargetInfo();
            LLVM.InitializeX86AsmParser();
            LLVM.InitializeX86AsmPrinter();

            root = rootFromSemmantic;
            packageManager = manager;
            debugScopeStack=new Stack<LLVMMetadataRef>();

            var moduleName = System.IO.Path.GetFileNameWithoutExtension(sourceFileNameAndPath);
            contextRef = CreateContext();
            moduleRef = contextRef.CreateModuleWithName(moduleName);
            
            debugBuilder = new CompilationDebugBuilder(debugInfo, this, sourceFileNameAndPath, CompilerVersion, targetTriple.Contains("msvc"));
            rootDebugScope = debugBuilder.RootScope;
            PushScope(root, rootDebugScope);

            currentNamespace = root;
            currentNamespace.pendingDefinitions = new Dictionary<string, IGlobalDefinition>();
            targetDataRef = moduleRef.GetDataLayout();

            foreach (var def in definitions)
            {
                if (!(def is AstUsingNamespace))
                {
                    foreach (var ident in def.Identifiers)
                    {
                        currentNamespace.pendingDefinitions.Add(ident.Name, def);
                    }
                }
            }

            foreach (var extra in extraNamespaces)
            {
                root.MergeSymbolTable(extra.symbols);
                foreach (var kv in extra.pass.UsedDefinitions)
                {
                    currentNamespace.pendingDefinitions.Add(kv.Key, kv.Value);
                }
            }
        }

        public UInt64 GetTypeSizeInBits(CompilationType type)
        {
            return targetDataRef.GetTypeSizeInBits(type.BackendType);
        }

        public UInt64 GetPointerSizeInBits()
        {
            return targetDataRef.GetPointerSizeInBits();
        }

        public void Compile()
        {
            while (currentNamespace.pendingDefinitions.Count!=0)
            {
                var enumerator = currentNamespace.pendingDefinitions.Keys.GetEnumerator();
                enumerator.MoveNext();
                CompileMissing(enumerator.Current);
            }

            debugBuilder.Finalise();
        }
        public bool CompileMissing(string identifier)
        {
            if (currentNamespace.pendingDefinitions.TryGetValue(identifier, out var definition))
            {
                var savedScope = PushScope(currentNamespace, rootDebugScope);
                foreach (var ident in definition.Identifiers)
                {
                    currentNamespace.pendingDefinitions.Remove(ident.Dump());
                }
                definition.Compile(this);
                PopScope(savedScope);
                return true;
            }
            return false;
        }

        public string Dump()
        {
            return moduleRef.PrintToString();
        }

        public CompilationType FetchArrayType(CompilationConstantIntegerKind numElements, CompilationType elementType, SourceLocation location)
        {
            uint num = (uint)numElements.Constant;
            return new CompilationArrayType(CreateArrayType(elementType.BackendType,num), elementType, num, debugBuilder, location);
        }

        public CompilationType FetchIntegerType(CompilationConstantIntegerKind numBits, SourceLocation location)
        {
            bool isSigned = false;
            if (numBits.Constant<BigInteger.Zero)
            {
                numBits.Negate();
                isSigned = true;
            }
            var num = (uint)numBits.Constant;
            return CreateIntegerType(num, isSigned, location);
        }

        public CompilationIntegerType FetchIntegerType(uint numBits, bool isSigned, SourceLocation location)
        {
            return CreateIntegerType(numBits, isSigned, location);
        }

        public (CompilationType compilationType, IType originalType) FetchNamedType(IIdentifier identifier)
        {
            var entry = currentScope.FetchType(identifier.Name);
            if (entry == null)
            {
                throw new System.NotSupportedException($"{identifier.Name} not found in current scope");
            }

            if (entry.Type == null)
            {
                CompileMissing(identifier.Name);
                entry= currentScope.FetchType(identifier.Name);
            }

            return (entry.Type, entry.AstType);
        }

        public void CreateNamedType(string identifier, CompilationType type, IType originalType)
        {
            var entry = currentScope.FetchType(identifier);
            if (entry == null)
            {
                throw new System.NotSupportedException($"{identifier} not found in current scope");
            }

            if (entry.Type != null)
            {
                throw new System.NotSupportedException($"{identifier} compilationType already defined");
            }
            var symbTabType = type.CopyAs(identifier);
            entry.SetCommpilationType(symbTabType);
        }

        public CompilationStructureType CreateNamedStruct(string identifier, SourceLocation location)
        {
            var backendType = contextRef.CreateNamedStruct(identifier);
            return new CompilationStructureType(backendType, debugBuilder, location);
        }

        public void FinaliseStruct(CompilationStructureType toFinalise, CompilationType[] elements, string[] names)
        {
            var types = new LLVMTypeRef[elements.Length];
            int idx = 0;
            foreach (var element in elements)
            {
                types[idx++] = element == null ? null : element.BackendType;
            }

            toFinalise.BackendType.StructSetBody(types, true);
            toFinalise.UpdateNamedStruct(elements, names);
        }

        public CompilationStructureType FetchStructType(CompilationType[] elements, string[] names, SourceLocation location)
        {
            var types = new LLVMTypeRef[elements.Length];
            int idx = 0;
            foreach (var element in elements)
            {
                types[idx++] = element == null ? null : element.BackendType;
            }

            var backendType = contextRef.GetStructType(types, true);
            return new CompilationStructureType(backendType, elements, names, debugBuilder, location);
        }

        public CompilationType FetchEnumType(CompilationType type, CompilationConstantIntegerKind[] values, Dictionary<string,uint> names, SourceLocation location)
        {
            return new CompilationEnumType(type, values, names, debugBuilder, location);
        }
        
        public CompilationType FetchAliasType(CompilationType type, CompilationType[][] values, string[][] names, uint [][] rotate, SourceLocation location)
        {
            return new CompilationAliasType(type.BackendType, type, values, names, rotate, debugBuilder, location);
        }


        public CompilationIntegerType CreateIntegerType(uint numBits, bool isSigned, SourceLocation location)
        {
            return new CompilationIntegerType( contextRef.GetIntType(numBits), isSigned, debugBuilder, location);
        }

        public CompilationPointerType CreatePointerType(CompilationType element, SourceLocation location)
        {
            return new CompilationPointerType(Extensions.Helpers.CreatePointerType(element.BackendType), element, debugBuilder, location);
        }

        public CompilationFunctionType CreateFunctionType(AstFunctionType functionType, CompilationParam[] inputs, CompilationParam[] outputs)
        {
            var allParams = new CompilationParam[inputs.Length + outputs.Length];
            var allBackendParams = new LLVMTypeRef[inputs.Length + outputs.Length];
            var paramIdx = 0;
            foreach(var i in inputs)
            {
                allBackendParams[paramIdx] = i.Type==null ? null : i.Type.BackendType;
                allParams[paramIdx++] = i;
            }
            foreach(var o in outputs)
            {
                //outputs need to be considered to be by ref
                allBackendParams[paramIdx] = CreatePointerType(o.Type, new SourceLocation()).BackendType;
                allParams[paramIdx++] = o;
            }

            var compilationFunctionType = Extensions.Helpers.CreateFunctionType(contextRef.VoidType, allBackendParams, false);
            return new CompilationFunctionType(compilationFunctionType, CompilationFunctionType.CallingConvention.HumphreyInternal, null, allParams, (uint)inputs.Length, debugBuilder, new SourceLocation(functionType.Token));
        }

        public CompilationFunctionType CreateExternalCFunctionType(AstFunctionType functionType, CompilationParam[] inputs, CompilationParam[] outputs)
        {
            var allParams = new CompilationParam[inputs.Length];
            var allBackendParams = new LLVMTypeRef[inputs.Length];
            var paramIdx = 0;
            foreach(var i in inputs)
            {
                allBackendParams[paramIdx] = i.Type==null ? null : i.Type.BackendType;
                allParams[paramIdx++] = i;
            }

            CompilationParam realReturn = default;
            LLVMTypeRef returnType = default;
            if (outputs.Length == 0)
            {
                returnType = contextRef.VoidType;
                realReturn = null;
            }
            else if (outputs.Length == 1)
            {
                returnType = outputs[0].Type == null ? null : outputs[0].Type.BackendType;
                realReturn = outputs[0];
            }
            else
            {
                //messages.Log(CompilerErrorKind.Error_ABIMismatch, $"C Calling Convention does not support multiple return values");
                throw new CompilationAbortException($"TODO - add error about multiple returns for C calling convention");
            }

            var compilationFunctionType = Extensions.Helpers.CreateFunctionType(returnType, allBackendParams, false);
            return new CompilationFunctionType(compilationFunctionType, CompilationFunctionType.CallingConvention.CDecl, realReturn, allParams, (uint)inputs.Length, debugBuilder, new SourceLocation(functionType.Token));
        }

        public CompilationValue FetchValueIfDefined(IIdentifier identifier, CompilationBuilder builder)
        {
            var entry = FetchCompilationValue(identifier.Name, builder);
            return entry;
        }

        CompilationValue FetchCompilationValue(string identifier, CompilationBuilder builder)
        {
            // Check for value
            var entry = currentScope.FetchValue(identifier);
            if (entry != null)
            {
                if (entry.Value == null)
                {
                    if (CompileMissing(identifier))
                    {
                        entry = currentScope.FetchValue(identifier);
                    }
                }
                if (entry.Value == null)
                    return null;
                var value = entry.Value;
                return builder.Load(value);
            }

            // Check for function - i guess we can construct this on the fly?
            entry = currentScope.FetchFunction(identifier);
            if (entry != null )
            {
                if (entry.Function == null)
                {
                    if (CompileMissing(identifier))
                    {
                        entry = currentScope.FetchFunction(identifier);
                    }
                }
                if (entry.Function == null)
                    return null;
                var function = entry.Function;
                return new CompilationValue(function.BackendValue, function.FunctionType, function.FunctionType.FrontendLocation);
            }

            // Check for named type (an enum is actually a value type) - might be better done at definition actually
            entry = currentScope.FetchType(identifier);
            if (entry != null)
            {
                if (entry.Type == null)
                {
                    if (CompileMissing(identifier))
                    {
                        entry = currentScope.FetchType(identifier);
                    }
                }
                // Catch external functions/function types 
                if (entry.Function!=null)
                {
                    var function=entry.Function;
                    return new CompilationValue(function.BackendValue, function.FunctionType, function.FunctionType.FrontendLocation);
                }
                if (entry.Type == null)
                    return null;
                var nType = entry.Type;
                return CreateUndef(nType);
            }

            return null;
        }

        public CompilationValue FetchValue(IIdentifier identifier, CompilationBuilder builder)
        {
            var resolved = FetchValueIfDefined(identifier, builder);
            if (resolved == null)
            {
                Messages.Log(CompilerErrorKind.Error_UndefinedValue, $"Undefined value '{identifier.Name}'", identifier.Token.Location, identifier.Token.Remainder);
            }
            return resolved;
        }

        public CompilationValue FetchLocation(string identifier, CompilationBuilder builder)
        {
            var entry = currentScope.FetchValue(identifier);
            if (entry == null)
            {
                throw new System.NotSupportedException($"{identifier} not found in current scope");
            }

            if (entry.Value == null)
            {
                if (CompileMissing(identifier))
                {
                    entry = currentScope.FetchValue(identifier);
                }
            }

            return entry.Value.Storage;
        }

        public CompilationBuilder CreateBuilder(CompilationFunction function, CompilationBlock bb)
        {
            var builder = contextRef.CreateBuilder();
            builder.PositionAtEnd(bb.BackendValue);
            return new CompilationBuilder(this, builder, function, bb);
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

        public LLVMValueRef CreateConstantArray(ICompilationConstantValue[] values, CompilationType elementType)
        {
            var constants = new LLVMValueRef[values.Length];
            for (int a = 0; a < values.Length;a++)
            {
                constants[a] = values[a].GetCompilationValue(this, elementType).BackendValue;
            }
            return CreateConstantArrayFromValues(constants, elementType.BackendType);
        }

        public CompilationValue CreateConstant(CompilationConstantIntegerKind constantValue, uint numBits, bool isSigned, SourceLocation location)
        {
            var constType = new CompilationIntegerType(contextRef.GetIntType(numBits), isSigned, debugBuilder, location);

            return new CompilationValue(constType.BackendType.CreateConstantValue(constantValue.Constant.ToString(), 10), constType, constantValue.FrontendLocation);
        }

        public CompilationValue CreateConstant(AstNumber decimalNumber, SourceLocation location)
        {
            var constantValue = new CompilationConstantIntegerKind(decimalNumber);
            var (numBits, isSigned) = constantValue.ComputeKind();

            return CreateConstant(constantValue, numBits, isSigned, location);
        }

        public CompilationValue CreateConstant(string decimalNumber, SourceLocation location)
        {
            return CreateConstant(new AstNumber(decimalNumber), location);
        }

        public CompilationValue CreateUndef(CompilationType type)
        {
            return new CompilationValue(type.BackendType.Undef, type, type.FrontendLocation);
        }

        public CompilationValue CreateZero(CompilationType type)
        {
            return new CompilationValue(type.BackendType.GetConstNull(), type, type.FrontendLocation);
        }

        public (CompilationFunction function, CommonSymbolTable symbolTable) BeginCreateGenericFunction(CompilationFunctionType type, AstIdentifier identifier)
        {
            // Generic specialisations are compile time named, so we must pre
            //declare them as the semantic pass does not generate specialisations

            // Declare the specialisation in the root for now
            var savedScope = PushScope(root, rootDebugScope);

            string functionName = identifier.Name;
            var predefinedValue = new CommonSymbolTableEntry(null,null);
            currentScope.AddFunction(functionName, predefinedValue);
            var function = CreateFunction(type, identifier, functionName, predefinedValue);
            return (function, savedScope);
        }

        public void EndCreateGenericFunction(CommonSymbolTable symbolTable)
        {
            PopScope(symbolTable);
        }

        public CompilationFunction CreateFunction(CompilationFunctionType type, AstIdentifier identifier)
        {
            string functionName = identifier.Name;

            var predefinedValue = currentScope.FetchFunction(functionName);
            if (predefinedValue == null)
                throw new Exception($"function {functionName} is missing from symbol table!");

            return CreateFunction(type, identifier, functionName, predefinedValue);
        }

        private CompilationFunction CreateFunction(CompilationFunctionType type, AstIdentifier identifier, string functionName, CommonSymbolTableEntry predefinedValue)
        {
            var func = moduleRef.AddFunction(functionName, type.BackendType);

            var cfunc = new CompilationFunction(func, type);

            predefinedValue.SetCommpilationFunction(cfunc);

            if (DebugInfoEnabled)
            {
                var debugFunction = debugBuilder.CreateDebugFunction(functionName, new SourceLocation(identifier.Token), type);

                cfunc.BackendValue.SetSubprogram(debugFunction);
            }
            return cfunc;
        }

        public CompilationFunction CreateExternalCFunction(CompilationFunctionType type, AstIdentifier identifier)
        {
            string functionName = identifier.Name;

            var predefinedValue = currentScope.FetchType(functionName);
            if (predefinedValue == null)
                throw new Exception($"function {functionName} is missing from symbol table!");

            var func = moduleRef.AddFunction(functionName, type.BackendType);
            func.Linkage = LLVMLinkage.LLVMExternalLinkage;

            var cfunc = new CompilationFunction(func, type);

            predefinedValue.SetCommpilationFunction(cfunc);

            return cfunc;
        }

        // Used for namespaces.. but we probably need a proper debug scope for this
        public (CommonSymbolTable oldScope, CommonSymbolTable oldNamespace) PushNamespaceScope(CommonSymbolTable newScope)
        {
            var oldNamespace = currentNamespace;
            currentNamespace = newScope;
            return (PushScope(newScope, debugScopeStack.Peek()), oldNamespace);
        }

        public void PopNamespaceScope((CommonSymbolTable oldScope, CommonSymbolTable oldNamespace) dc)
        {
            currentNamespace = dc.oldNamespace;
            PopScope(dc.oldScope);
        }

        public CommonSymbolTable PushScope(CommonSymbolTable newScope, LLVMMetadataRef debugScope)
        {
            debugScopeStack.Push(debugScope);
            var oldScope = currentScope;
            currentScope = newScope;
            currentScope.PatchScope(root);
            currentScope.FixParent(oldScope);
            return oldScope;
        }

        public void PopScope(CommonSymbolTable newScope)
        {
            currentScope = newScope;
            debugScopeStack.Pop();
        }

        public (CompilationValue cv, CompilationType ct) CreateGlobalVariable(CompilationType type, AstIdentifier identifier, SourceLocation location, ICompilationConstantValue initialiser = null)
        {
            var ident = identifier.Name;

            var predefinedValue = currentScope.FetchValue(ident);
            if (predefinedValue == null)
                throw new Exception($"global value {identifier} is missing in symbol table!");

            if (type is CompilationFunctionType)
            {
                type = CreatePointerType(type, type.Location);
            }

            var global = moduleRef.AddGlobal(type.BackendType, ident);

            if (initialiser != null)
            {
                var constantValue = initialiser.GetCompilationValue(this, type);
                global.Initializer = constantValue.BackendValue;
            }

            var globalValue = new CompilationValue(global, type, identifier.Token);
            globalValue.Storage = new CompilationValue(global, CreatePointerType(type, location), identifier.Token);

            predefinedValue.SetCommpilationValue(globalValue);

            return (globalValue,type);
        }

        public (CompilationValue cv, CompilationType ct) CreateLocalVariable(CompilationUnit unit, CompilationBuilder builder, CompilationBuilder localBuilder, CompilationType type, AstIdentifier identifier, ICompilationValue initialiser, Result<Tokens> location)
        {
            var ident = identifier.Name;

            var predefinedValue = currentScope.FetchValue(ident);
            if (predefinedValue == null)
                throw new Exception($"local value {identifier} is missing in symbol table!");

            if (type is CompilationFunctionType)
            {
                type = CreatePointerType(type, type.Location);
            }

            var local = localBuilder.Alloca(type);

            if (initialiser != null)
            {
                var value = AstUnaryExpression.EnsureTypeOk(unit, builder, initialiser, type, location);
                builder.Store(value, local);
            }
            local.Storage = new CompilationValue(local.BackendValue, CreatePointerType(type, new SourceLocation(location)), identifier.Token);

            predefinedValue.SetCommpilationValue(local);

            return (local, type);
        }

        public CompilationParam CreateFunctionParameter(CompilationType type, AstIdentifier identifier)
        {
            if (type is CompilationFunctionType)
            {
                type = CreatePointerType(type, type.Location);
            }
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

            return ee.GetPointerToGlobal(root.FetchFunction(identifier).Function.BackendValue);
        }

        public bool EmitToBitCodeFile(string filename)
        {
            moduleRef.WriteBitcodeToFile(filename);
            return true;
        }

        public bool EmitToFile(string filename, bool pic, bool kernel)
        {
            LLVMRelocMode reloc = LLVMRelocMode.LLVMRelocDefault;
            if (pic)
                reloc = LLVMRelocMode.LLVMRelocPIC;
            LLVMCodeModel model = LLVMCodeModel.LLVMCodeModelDefault;
            if (kernel)
                model = LLVMCodeModel.LLVMCodeModelKernel;
            LLVMCodeGenOptLevel codeGenLevel = LLVMCodeGenOptLevel.LLVMCodeGenLevelNone;
            if (optimisations)
                codeGenLevel=LLVMCodeGenOptLevel.LLVMCodeGenLevelAggressive;

            var targetMachine = LLVMTargetRef.First.CreateTargetMachine(targetTriple, "generic", kernel?"-sse,-mmx":"", codeGenLevel, reloc, model);

            moduleRef.SetDataLayout(targetMachine.CreateTargetDataLayout());
            moduleRef.Target = LLVMTargetRef.DefaultTriple;

            var pm = LLVMPassManagerRef.Create();
            if (optimisations)
            {
                Optimise(pm);
            }

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

        public LLVMMetadataRef CreateDebugLocationMeta(SourceLocation location)
        {
            // We don't bother with the column for source locations as we are only doing per statement line location information anyway
            return moduleRef.Context.CreateDebugLocation(location.StartLine, 0/*location.StartColumn*/, debugScopeStack.Peek(), default(LLVMMetadataRef));
        }

        public LLVMValueRef CreateDebugLocation(SourceLocation location)
        {
            var meta = CreateDebugLocationMeta(location);
            return meta.AsValue(contextRef);
        }

        public LLVMMetadataRef CreateParameterVariable(string name, uint argNo, SourceLocation location, CompilationDebugType debugType)
        {
            if (debugBuilder.Enabled)
            {
                return debugBuilder.CreateParameterVariable(name, debugScopeStack.Peek(), location, argNo, debugType.BackendType);
            }
            return default;
        }

        public bool DumpLLVM(bool pic, bool kernel)
        {
            LLVMRelocMode reloc = LLVMRelocMode.LLVMRelocDefault;
            if (pic)
                reloc = LLVMRelocMode.LLVMRelocPIC;
            LLVMCodeModel model = LLVMCodeModel.LLVMCodeModelDefault;
            if (kernel)
                model = LLVMCodeModel.LLVMCodeModelKernel;
            LLVMCodeGenOptLevel codeGenLevel = LLVMCodeGenOptLevel.LLVMCodeGenLevelNone;
            if (optimisations)
                codeGenLevel=LLVMCodeGenOptLevel.LLVMCodeGenLevelAggressive;

            var targetMachine = LLVMTargetRef.First.CreateTargetMachine(targetTriple, "generic",  kernel?"-sse,-mmx":"", codeGenLevel, reloc, model);

            moduleRef.SetDataLayout(targetMachine.CreateTargetDataLayout());
            moduleRef.Target = LLVMTargetRef.DefaultTriple;

            var pm = LLVMPassManagerRef.Create();
            if (optimisations)
            {
                Optimise(pm);
            }

            if (!moduleRef.TryVerify(LLVMVerifierFailureAction.LLVMPrintMessageAction, out var message))
            {
                messages.Log(CompilerErrorKind.Error_FailedVerification, $"Module Verification Failed : {moduleRef.PrintToString()}{Environment.NewLine}{message}");
                return false;
            }

            pm.Run(moduleRef);

            Console.WriteLine(moduleRef.PrintToString());

            return true;
        }

        public void Optimise(LLVMPassManagerRef passManagerRef)
        {
            var passes = PassManagerBuilderCreate();
            passes.PopulateModulePassManager(passManagerRef);
            passes.PopulateFunctionPassManager(passManagerRef);
        }

        public bool DumpDisassembly(bool pic, bool kernel)
        {
            LLVMRelocMode reloc = LLVMRelocMode.LLVMRelocDefault;
            if (pic)
                reloc = LLVMRelocMode.LLVMRelocPIC;
            LLVMCodeModel model = LLVMCodeModel.LLVMCodeModelDefault;
            if (kernel)
                model = LLVMCodeModel.LLVMCodeModelKernel;
            LLVMCodeGenOptLevel codeGenLevel = LLVMCodeGenOptLevel.LLVMCodeGenLevelNone;
            if (optimisations)
                codeGenLevel=LLVMCodeGenOptLevel.LLVMCodeGenLevelAggressive;

            var targetMachine = LLVMTargetRef.First.CreateTargetMachine(targetTriple, "generic",  kernel?"-sse,-mmx":"", codeGenLevel, reloc, model);

            moduleRef.SetDataLayout(targetMachine.CreateTargetDataLayout());
            moduleRef.Target = LLVMTargetRef.DefaultTriple;


            var pm = LLVMPassManagerRef.Create();
            if (optimisations)
            {
                Optimise(pm);
            }

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
            moduleRef.AddNamedMetadataOperand(key,LLVMValueRef.CreateMDNode(new[] { moduleRef.Context.GetMDString(value) }));
        }

        public LLVMBasicBlockRef AppendNewBasicBlockToFunction(CompilationFunction function, string basicBlockName)
        {
            return contextRef.AppendBasicBlock(function.BackendValue, basicBlockName);
        }

        public LLVMMetadataRef CreateDebugScope(SourceLocation location)
        {
            return debugBuilder.CreateLexicalScope(debugScopeStack.Peek(), location);
        }

        public LLVMMetadataRef GetScope(CompilationFunction function)
        {
            return function.BackendValue.GetSubprogram();
        }

        public void InsertDeclareAtEnd(CompilationValue storage, LLVMMetadataRef varInfo, SourceLocation location, CompilationBlock block)
        {
            debugBuilder.InsertDeclareAtEnd(storage, varInfo, location, block);
        }

        public LLVMMetadataRef CreateAutoVariable(string name, SourceLocation location, CompilationDebugType type)
        {
            return debugBuilder.CreateAutoVariable(name, debugScopeStack.Peek(), location, type);
        }

        public LLVMMetadataRef CreateGlobalVariableExpression(string name, SourceLocation location, CompilationDebugType type)
        {
            return debugBuilder.CreateGlobalVarable(name, debugScopeStack.Peek(), location, type);
        }

        public CommonSymbolTable CurrentScope => currentScope;
        public LLVMMetadataRef DebugScope => debugScopeStack.Peek();

        public LLVMModuleRef Module => moduleRef;
        public CompilerMessages Messages => messages;

        public bool DebugInfoEnabled => debugBuilder.Enabled;
    }
}