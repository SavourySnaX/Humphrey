using Extensions;
using Humphrey.Backend;
using LLVMSharp.Interop;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace Humphrey.Compiler.src.Backend.ABI
{

    public interface CABI
    {
        List<ArgInfo> ComputeTransform(CompilationUnit unit, CompilationFunctionType functionType);
        uint getTypeRequiredAlign(CompilationUnit unit, LLVMTypeRef type);

        //public LLVMTypeRef TransformType(CompilationUnit unit, LLVMTypeRef type);
        FunctionIRMapping GetFunctionIRMapping(List<ArgInfo> argInfoArray)
        {
            var functionIRMapping = new FunctionIRMapping();

            uint irArgumentNumber = 0;

            functionIRMapping.setReturnArgInfo(argInfoArray[0]);

            var returnArgInfo = functionIRMapping.ReturnArgInfo;

            if (returnArgInfo.getKind() == ArgInfo.EArgKind.Indirect)
            {
                functionIRMapping.setStructRetArgIndex(irArgumentNumber++);
            }

            for (int argumentNumber = 1; argumentNumber < argInfoArray.Count; argumentNumber++)
            {
                var argInfo = argInfoArray[argumentNumber];

                var argumentIRMapping = new ArgumentIRMapping();
                argumentIRMapping.argInfo = argInfo;

                if (argInfo.PaddingType.Kind != LLVMTypeKind.LLVMVoidTypeKind)
                {
                    argumentIRMapping.paddingArgIndex = irArgumentNumber++;
                }

                switch (argInfo.getKind())
                {
                    case ArgInfo.EArgKind.Extend:
                    case ArgInfo.EArgKind.Direct:
                        {
                            // FIXME: handle sseregparm someday...
                            var coerceType = argInfo.CoerceType;
                            if (argInfo.IsDirect && argInfo.CanBeFlattened && coerceType.Kind == LLVMTypeKind.LLVMStructTypeKind)
                            {
                                argumentIRMapping.numberOfIRArgs = coerceType.StructElementTypesCount;
                            }
                            else
                            {
                                argumentIRMapping.numberOfIRArgs = 1;
                            }
                            break;
                        }
                    case ArgInfo.EArgKind.Indirect:
                    case ArgInfo.EArgKind.Cast:
                        argumentIRMapping.numberOfIRArgs = 1;
                        break;
                    case ArgInfo.EArgKind.Ignore:
                    case ArgInfo.EArgKind.InAlloca:
                        // ignore and inalloca doesn't have matching LLVM parameters.
                        argumentIRMapping.numberOfIRArgs = 0;
                        break;
                    case ArgInfo.EArgKind.Expand:
                        {
                            argumentIRMapping.numberOfIRArgs = getExpansionSize(argInfo.getExpandType());
                            break;
                        }
                }

                if (argumentIRMapping.numberOfIRArgs > 0)
                {
                    argumentIRMapping.firstArgIndex = irArgumentNumber;
                    irArgumentNumber += argumentIRMapping.numberOfIRArgs;
                }

                functionIRMapping.Arguments.Add(argumentIRMapping);
            }

            // 		if (FI.usesInAlloca()) {
            // 			functionIRMapping.setInallocaArgIndex(irArgumentNumber++);
            // 		}

            functionIRMapping.setTotalIRArgs(irArgumentNumber);

            return functionIRMapping;

        }

        uint getExpansionSize(LLVMTypeRef type)
        {
            if (type.Kind == LLVMTypeKind.LLVMVoidTypeKind)
            {
                throw new System.ArgumentException("Should not be called with void type");
            }

            if (type.Kind == LLVMTypeKind.LLVMArrayTypeKind)
            {
                return type.ArrayLength * getExpansionSize(type.ElementType);
            }

            if (type.Kind == LLVMTypeKind.LLVMStructTypeKind)
            {
                uint result = 0;
                var structElementTypes = type.GetStructElementTypes();
                foreach (var field in structElementTypes)
                {
                    result += getExpansionSize(field);
                }
                return result;
            }

            return 1;
        }

        LLVMTypeRef getFunctionType(LLVMContextRef context, LLVMTypeRef returnType, LLVMTypeRef[] paramTypes, FunctionIRMapping functionIRMapping)
        {
            LLVMTypeRef resultType = default;

            var returnArgInfo = functionIRMapping.ReturnArgInfo;
            switch (returnArgInfo.getKind())
            {
                case ArgInfo.EArgKind.Expand:
                    throw new System.ArgumentException("Invalid ABI kind for return argument");

                case ArgInfo.EArgKind.Cast:
                case ArgInfo.EArgKind.Extend:
                case ArgInfo.EArgKind.Direct:
                    resultType = returnArgInfo.CoerceType;
                    break;

                case ArgInfo.EArgKind.InAlloca:
                    {
                        if (returnArgInfo.IsInAllocaSRet)
                        {
                            // sret things on win32 aren't void, they return the sret pointer.
                            var pointeeType = returnType;
                            resultType = LLVMTypeRef.CreatePointer(pointeeType, 0);
                        }
                        else
                        {
                            resultType = context.VoidType;
                        }
                    }
                    break;

                case ArgInfo.EArgKind.Indirect:
                    {
                        //assert(!returnArgInfo.getIndirectAlign() && "Align unused on indirect return.");
                        resultType = context.VoidType;
                        break;
                    }

                case ArgInfo.EArgKind.Ignore:
                    resultType = returnType;
                    break;
            }

            var argumentTypes = new LLVMTypeRef[functionIRMapping.TotalIRArgs];

            // Add type for sret argument.
            if (functionIRMapping.HasStructRetArg)
            {
                var pointeeType = returnType;
                argumentTypes[functionIRMapping.StructRetArgIndex] = LLVMTypeRef.CreatePointer(pointeeType, 0);
            }

            // Add type for inalloca argument.
            if (functionIRMapping.HasInAllocaArg)
            {
                throw new System.NotImplementedException("TODO");
                // 			auto argStruct = FI.getArgStruct();
                // 			assert(argStruct);
                // 			argumentTypes[functionIRMapping.inallocaArgNo()] = argStruct->getPointerTo();
            }

            // Add in all of the required arguments.
            for (int argumentNumber = 0; argumentNumber < functionIRMapping.Arguments.Count; argumentNumber++)
            {
                var argInfo = functionIRMapping.Arguments[argumentNumber].argInfo;
                var argumentType = paramTypes[argumentNumber];

                // Insert a padding type to ensure proper alignment.
                if (functionIRMapping.hasPaddingArg(argumentNumber))
                {
                    paramTypes[functionIRMapping.paddingArgIndex(argumentNumber)] = argInfo.PaddingType;
                }

                uint firstIRArg, numIRArgs;
                (firstIRArg, numIRArgs) = functionIRMapping.getIRArgRange(argumentNumber);

                switch (argInfo.getKind())
                {
                    case ArgInfo.EArgKind.Ignore:
                    case ArgInfo.EArgKind.InAlloca:
                        if (numIRArgs != 0)
                        {
                            throw new System.ArgumentException("Invalid number of IR args for Ignore or InAlloca");
                        }
                        break;

                    case ArgInfo.EArgKind.Indirect:
                        {
                            if (numIRArgs != 1)
                            {
                                throw new System.ArgumentException("Invalid number of IR args for Indirect");
                            }
                            // Indirect arguments are always on
                            // the stack, which is addr space #0.
                            argumentTypes[firstIRArg] = Extensions.Helpers.CreatePointerType(argumentType);
                            break;
                        }

                    case ArgInfo.EArgKind.Cast:
                        {
                            argumentTypes[firstIRArg] = context.Int64Type;
                        }
                        break;
                    case ArgInfo.EArgKind.Extend:
                    case ArgInfo.EArgKind.Direct:
                        {
                            // Fast-isel and the optimizer generally like scalar values better than
                            // FCAs, so we flatten them if this is safe to do for this argument.
                            var coerceType = argInfo.CoerceType;
                            if (coerceType.Kind == LLVMTypeKind.LLVMStructTypeKind && argInfo.IsDirect && argInfo.CanBeFlattened)
                            {
                                if (numIRArgs != coerceType.StructElementTypesCount)
                                {
                                    throw new System.ArgumentException("Invalid number of IR args for Direct");
                                }
                                var structElementTypes = coerceType.GetStructElementTypes();
                                foreach (var member in structElementTypes)
                                {
                                    argumentTypes[firstIRArg++] = member;
                                }
                            }
                            else
                            {
                                if (numIRArgs != 1)
                                {
                                    throw new System.ArgumentException("Invalid number of IR args for Direct");
                                }
                                argumentTypes[firstIRArg] = coerceType;
                            }
                            break;
                        }

                    case ArgInfo.EArgKind.Expand:
                        throw new System.NotImplementedException("TODO");
                        /*
                        var argumentTypesIter = argumentTypes.begin() + firstIRArg;
                        getExpandedTypes(typeInfo,
                                         argInfo.getExpandType(),
                                         argumentTypesIter);
                        assert(argumentTypesIter == argumentTypes.begin() + firstIRArg + numIRArgs);
                        break;*/
                }
            }

            return Extensions.Helpers.CreateFunctionType(resultType, argumentTypes, false);
        }

    }

    public class WindowsX64_C_ABI : CABI
    {
        public List<ArgInfo> ComputeTransform(CompilationUnit unit, CompilationFunctionType functionType)
        {
            var args = new List<ArgInfo>();

            static ArgInfo Transform(CompilationUnit unit , LLVMTypeRef type)
            {
                if (type.IsIntegralType())
                {
                    var size = unit.Module.GetDataLayout().ABISizeOfType(type);
                    if (size<=8)
                    {
                        return ArgInfo.getDirect(unit, type);
                    }
                    return ArgInfo.getIndirect(unit, unit.Module.GetDataLayout().ABIAlignmentOfType(type), false, false);
                }
                else if (type.IsAggregateType())
                {
                    var size = unit.Module.GetDataLayout().ABISizeOfType(type);
                    if (size<=8)
                    {
                        return ArgInfo.getCast(unit, type, (uint)size);
                    }
                    return ArgInfo.getIndirect(unit, unit.Module.GetDataLayout().ABIAlignmentOfType(type), false, false);
                }
                else
                {
                    throw new System.NotImplementedException($"TODO {type.Kind} is not supported.");
                }
            }

            if (functionType.ReturnType == null)
            {
                args.Add(ArgInfo.getIgnore());
            }
            else
            {
                args.Add(Transform(unit, functionType.ReturnType.Type.BackendType));
            }

            foreach (var arg in functionType.Parameters)
            {
                args.Add(Transform(unit, arg.Type.BackendType));
            }

            return args;
        }

        public uint getTypeRequiredAlign(CompilationUnit unit, LLVMTypeRef type)
        {
            switch (type.Kind)
            {
                case LLVMTypeKind.LLVMVoidTypeKind:
                    return 1;
                case LLVMTypeKind.LLVMPointerTypeKind:
                    return 8;
                case LLVMTypeKind.LLVMIntegerTypeKind:
                    return LLVMHelper.roundUpToPower2(type.IntWidth / 8);
                case LLVMTypeKind.LLVMFloatTypeKind:
                    return 4;
                case LLVMTypeKind.LLVMDoubleTypeKind:
                    return 8;
                case LLVMTypeKind.LLVMStructTypeKind:
                    {
                        uint align = 1;
                        var structElementTypes = type.GetStructElementTypes();
                        for (int a = 0; a < type.StructElementTypesCount; a++)
                        {
                            var elementAlign = getTypeRequiredAlign(unit, structElementTypes[a]);
                            if (elementAlign > align)
                            {
                                align = elementAlign;
                            }
                        }
                        return align;
                    }
                case LLVMTypeKind.LLVMArrayTypeKind:
                    return getTypeRequiredAlign(unit, type.ElementType);
                case LLVMTypeKind.LLVMVectorTypeKind:
                    {
                        uint elementAlign = getTypeRequiredAlign(unit, type.ElementType);
                        uint minAlign = unit.Module.GetDataLayout().GetTypeAllocSize(type) >= 32 ? 32u : unit.Module.GetDataLayout().GetTypeAllocSize(type) >= 16 ? 16u : 1u;
                        return elementAlign > minAlign ? elementAlign : minAlign;
                    }
                default:
                    throw new System.NotImplementedException("TODO");
            }
        }


    }


    public struct FunctionIRMapping
    {
        ArgInfo returnArgInfo;
        uint structRetArgIndex;
        bool hasStructRetArg;
        bool hasInallocaArg;
        uint totalIRArgs;

        public ArgInfo ReturnArgInfo => returnArgInfo;

        public List<ArgumentIRMapping> Arguments;

        public FunctionIRMapping()
        {
            returnArgInfo = default;
            structRetArgIndex = 0;
            totalIRArgs = 0;
            hasStructRetArg = false;
            hasInallocaArg = false;
            Arguments = new List<ArgumentIRMapping>();
        }

        public void setReturnArgInfo(ArgInfo argInfo)
        {
            returnArgInfo = argInfo;
        }

        public void setStructRetArgIndex(uint index)
        {
            structRetArgIndex = index;
            hasStructRetArg = true;
        }

        public void setTotalIRArgs(uint total)
        {
            totalIRArgs = total;
        }

        public bool hasPaddingArg(int argumentNumber)
        {
            return Arguments[argumentNumber].paddingArgIndex != 0;
        }

        public uint paddingArgIndex(int argumentNumber)
        {
            return Arguments[argumentNumber].paddingArgIndex;
        }

        public (uint, uint) getIRArgRange(int argumentNumber)
        {
            return (Arguments[argumentNumber].firstArgIndex, Arguments[argumentNumber].numberOfIRArgs);
        }

        public uint TotalIRArgs => totalIRArgs;
        public bool HasStructRetArg => hasStructRetArg;
        public bool HasInAllocaArg => hasInallocaArg;
        public uint StructRetArgIndex => structRetArgIndex;
    }

    public struct ArgumentIRMapping
    {
        public ArgumentIRMapping()
        {
            argInfo = default;
            paddingArgIndex = 0;
            firstArgIndex = 0;
            numberOfIRArgs = 0;
        }
        public ArgInfo argInfo;
        public uint paddingArgIndex;
        public uint firstArgIndex;
        public uint numberOfIRArgs;

    }
    public class Caller
    {
        private LLVMTypeRef _functionType;
        private LLVMValueRef _function;
        private LLVMValueRef[] _backendValues;
        private FunctionIRMapping _mapping;
        private LLVMBuilderRef _builder;
        private LLVMBuilderRef _locals;
        private CompilationUnit _unit;
        private CABI _targetABI;

        public Caller(CABI targetABI, LLVMTypeRef functionType, LLVMValueRef function, LLVMValueRef[] backendValues, FunctionIRMapping mapping, LLVMBuilderRef builder, LLVMBuilderRef locals, CompilationUnit unit)
        {
            _targetABI = targetABI;
            _functionType = functionType;
            _function = function;
            _backendValues = backendValues;
            _mapping = mapping;
            _builder = builder;
            _locals = locals;
            _unit = unit;
        }

        LLVMValueRef createTempAlloca(LLVMTypeRef type)
        {
            var result = _builder.CreateAlloca(_locals, type, "abi_TempAlloc");
            return result;
        }

        LLVMValueRef createMemTemp(LLVMTypeRef type)
        {
            var result = _builder.CreateAlloca(_locals, type, "abi_MemTemp");
            result.Alignment = getTypeRequiredAlign(type);
            return result;
        }

        public List<LLVMValueRef> encodeArguments(CompilationFunctionType funcType)
        {
            var args = new LLVMValueRef[_mapping.TotalIRArgs];

            var returnArgInfo = _mapping.ReturnArgInfo;
//            LLVMValueRef argMemory = default;
            LLVMValueRef structRetPtr = default;

            if (returnArgInfo.IsIndirect || returnArgInfo.IsInAllocaSRet)
            {
                structRetPtr = createMemTemp(funcType.ReturnType.Type.BackendType);
                if (_mapping.HasStructRetArg)
                {
                    args[_mapping.StructRetArgIndex] = structRetPtr;
                }
                else
                {
                    throw new System.NotImplementedException("TODO");
                }
            }

            for (int argumentNumber = 0; argumentNumber < _backendValues.Length; argumentNumber++)
            {
                var argumentValue = _backendValues[argumentNumber];
                var argumentType = funcType.Parameters[argumentNumber].Type;
                var argInfo = _mapping.Arguments[argumentNumber].argInfo;

                var isArgumentInMemory = false;

                if (_mapping.hasPaddingArg(argumentNumber))
                {
                    args[_mapping.paddingArgIndex(argumentNumber)] = argInfo.PaddingType.Undef;
                }

                uint firstIRArg, numIRArgs;
                (firstIRArg, numIRArgs) = _mapping.getIRArgRange(argumentNumber);

                switch (argInfo.getKind())
                {
                    case ArgInfo.EArgKind.InAlloca:
                        throw new System.NotImplementedException("TODO");
                    /*
                    assert(numIRArgs == 0);
                    assert(getTarget().getTriple().getArch() == llvm::Triple::x86);
                    if (isArgumentInMemory)
                    {
                        // Replace the placeholder with the appropriate argument slot GEP.
                        llvm_unreachable("TODO");
                    }
                    else
                    {
                        // Store the RValue into the argument struct.
                        llvm_unreachable("TODO");
                    }
                    break;*/

                    case ArgInfo.EArgKind.Indirect:
                        if (numIRArgs != 1)
                        {
                            throw new System.ArgumentException("Invalid number of IR args for Indirect");
                        }
                        if (!isArgumentInMemory)
                        {
                            var allocInst = createMemTemp(argumentType.BackendType);
                            if (argInfo.IndirectAlign > allocInst.Alignment)
                            {
                                allocInst.Alignment = argInfo.IndirectAlign;
                            }
                            args[firstIRArg] = allocInst;

                            var storeInst = _builder.BuildStore(argumentValue, allocInst);
                            storeInst.Alignment = allocInst.Alignment;
                        }
                        else
                        {
                            // We want to avoid creating an unnecessary temporary+copy here;
                            // however, we need one in three cases:
                            // 1. If the argument is not byval, and we are required to copy the
                            //      source.    (This case doesn't occur on any common architecture.)
                            // 2. If the argument is byval, RV is not sufficiently aligned, and
                            //      we cannot force it to be sufficiently aligned.
                            // 3. If the argument is byval, but RV is located in an address space
                            //      different than that of the argument (0).
                            throw new System.NotImplementedException("TODO");
                        }
                        break;

                    case ArgInfo.EArgKind.Ignore:
                        if (numIRArgs != 0)
                        {
                            throw new System.ArgumentException("Invalid number of IR args for Ignore");
                        }
                        break;

                    case ArgInfo.EArgKind.Cast:
                        {
                            var address = createMemTemp(argumentType.BackendType);
                            if (argInfo.IndirectAlign > address.Alignment)
                            {
                                address.Alignment = argInfo.IndirectAlign;
                            }
                            _builder.BuildStore(argumentValue, address);
                            var I64 = _unit.CreateIntegerType(64, false, new SourceLocation());
                            var pI64 = _unit.CreatePointerType(I64, new SourceLocation());
                            var asI64 = _builder.BuildBitCast(address, pI64.BackendType);
                            var loadedValue = _builder.BuildLoad2(I64.BackendType, asI64);
                            args[firstIRArg] = loadedValue;

                        }
                        break;

                    case ArgInfo.EArgKind.Extend:
                    case ArgInfo.EArgKind.Direct:
                        {
                            var coerceType = argInfo.CoerceType;

                            if ((coerceType.Kind == LLVMTypeKind.LLVMStructTypeKind) && (coerceType.Kind == argumentType.BackendType.Kind) && (argInfo.DirectOffset == 0))
                            {
                                if (numIRArgs != 1)
                                {
                                    throw new System.ArgumentException("Invalid number of IR args for Direct");
                                }

                                var value = argumentValue;
                                var llvmArgType = argumentType.BackendType;

                                if (llvmArgType != value.TypeOf)
                                {
                                    if (value.TypeOf.Kind == LLVMTypeKind.LLVMIntegerTypeKind)
                                    {
                                        value = _builder.BuildZExt(value, llvmArgType);
                                    }
                                    if (firstIRArg < _mapping.TotalIRArgs)
                                    {
                                        value = _builder.BuildBitCast(value, llvmArgType);
                                    }
                                }

                                args[firstIRArg] = value;
                                break;
                            }

                            LLVMValueRef sourcePtr = default;
                            if (!isArgumentInMemory)
                            {
                                sourcePtr = createMemTemp(argumentType.BackendType);
                                _builder.BuildStore(argumentValue, sourcePtr);
                            }
                            else
                            {
                                sourcePtr = argumentValue;
                            }

                            if (argInfo.DirectOffset != 0)
                            {
                                sourcePtr = _builder.BuildBitCast(sourcePtr, LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0));
                                //sourcePtr = builder_.getBuilder().CreateConstGEP1_32(sourcePtr, argInfo.getDirectOffset());
                                //sourcePtr = builder_.getBuilder().CreateBitCast(sourcePtr, llvm::PointerType::getUnqual(typeInfo_.getLLVMType(coerceType)));
                            }

                            if (coerceType.Kind == LLVMTypeKind.LLVMStructTypeKind && argInfo.IsDirect && argInfo.CanBeFlattened)
                            {
                                var sourceSize = _unit.Module.GetDataLayout().GetTypeAllocSize(argumentType.BackendType);
                                var destSize = _unit.Module.GetDataLayout().GetTypeAllocSize(coerceType);

                                if (sourceSize < destSize)
                                {
                                    var tempAlloca = createMemTemp(coerceType);
                                    //MEMCPY (tempAlloca, sourcePtr, sourceSize)
                                    sourcePtr = tempAlloca;
                                }
                                else
                                {
                                    sourcePtr = _builder.BuildBitCast(sourcePtr, LLVMTypeRef.CreatePointer(coerceType, 0));
                                }

                                if (numIRArgs != coerceType.StructElementTypesCount)
                                {
                                    throw new System.ArgumentException("Invalid number of IR args for Direct");
                                }

                                for (int a = 0; a < coerceType.StructElementTypesCount; a++)
                                {
                                    throw new System.NotImplementedException("TODO");
                                    /*
                                    const auto elementPtr = createConstGEP2_32(builder_,
                                                                                                   typeInfo_.getLLVMType(coerceType),
                                                                                                   sourcePtr,
                                                                                                   0, i);
                                    const auto loadInst = builder_.getBuilder().CreateLoad(elementPtr);
                                    // We don't know what we're loading from.
                                    loadInst->setAlignment(1);
                                    irCallArgs[firstIRArg + i] = loadInst;*/
                                }
                            }
                            else
                            {
                                if (numIRArgs != 1)
                                {
                                    throw new System.ArgumentException("Invalid number of IR args for Direct");
                                }
                                args[firstIRArg] = createCoercedLoad(sourcePtr, argumentType.BackendType, coerceType);
                            }
                            break;
                        }
                    case ArgInfo.EArgKind.Expand:
                        {
                            var alloca = createMemTemp(argumentType.BackendType);
                            var storeInst = _builder.BuildStore(argumentValue, alloca);
                            throw new System.NotImplementedException("TODO");
                            /*
                            storeInst.Alignment = getTypeRequiredAlign(argumentType);

                            storeInst->setAlignment(typeInfo_.getTypeRequiredAlign(argumentType).asBytes());

                            auto iterator = irCallArgs.begin() + firstIRArg;
                            expandTypeToArgs(typeInfo_,
                                            builder_,
                                            argumentType,
                                            alloca,
                                            iterator);
                            assert(iterator == irCallArgs.begin() + firstIRArg + numIRArgs);
                            break;*/
                        }
                }
            }

            return args.ToList();
        }

        private LLVMValueRef createCoercedLoad(LLVMValueRef sourcePtr, LLVMTypeRef sourceType, LLVMTypeRef destType)
        {
            if (LLVMHelper.typesAreEqual(sourceType, destType))
            {
                return _builder.BuildLoad2(sourceType, sourcePtr);
            }

            var destSize = _unit.Module.GetDataLayout().GetTypeAllocSize(destType);

            if (sourceType.Kind == LLVMTypeKind.LLVMStructTypeKind)
            {
                (sourcePtr, sourceType) = enterStructPointerForCoercedAccess(sourcePtr, sourceType, destSize);
            }

            var sourceSize = _unit.Module.GetDataLayout().GetTypeAllocSize(sourceType);

            if ((destType.Kind == LLVMTypeKind.LLVMIntegerTypeKind || destType.Kind == LLVMTypeKind.LLVMPointerTypeKind) &&
                 (sourceType.Kind == LLVMTypeKind.LLVMIntegerTypeKind || sourceType.Kind == LLVMTypeKind.LLVMPointerTypeKind))
            {
                throw new System.NotImplementedException("TODO");
                /*
                var loadInst = _builder.BuildLoad2(sourceType, sourcePtr);
                return coerceIntOrPtrToIntOrPtr(loadInst, sourceType, destType);*/
            }

            if (sourceSize >= destSize)
            {
                var asPointer = LLVMTypeRef.CreatePointer(destType, 0);
                var casted = _builder.BuildBitCast(sourcePtr, asPointer);
                var loadInst = _builder.BuildLoad2(destType, casted);
                loadInst.Alignment = 1;
                return loadInst;
            }
            else
            {
                throw new System.NotImplementedException("TODO");
                /*
                var tmpAlloca = createTempAlloca(destType);
                var i8PtrType = LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0);
                var casted = _builder.BuildBitCast(tmpAlloca, i8PtrType);
                var sourceCasted = _builder.BuildBitCast(sourcePtr, i8PtrType);
                _builder.BuildMemCpy(casted, sourceCasted, sourceSize, 1);
                return _builder.BuildLoad2(destType, tmpAlloca);*/
            }

        }

        public (LLVMValueRef value, LLVMTypeRef type) enterStructPointerForCoercedAccess(LLVMValueRef sourcePtr, LLVMTypeRef sourceType, ulong destSize)
        {
            if (sourceType.StructElementTypesCount == 0)
            {
                return (sourcePtr, sourceType);
            }

            var structElementTypes = sourceType.GetStructElementTypes();
            var firstElementType = structElementTypes[0];

            var typeStoreSize = _unit.Module.GetDataLayout().GetTypeAllocSize(sourceType);
            var firstElementSize = _unit.Module.GetDataLayout().GetTypeAllocSize(firstElementType);
            if (firstElementSize < destSize && firstElementSize < typeStoreSize)
            {
                return (sourcePtr, sourceType);
            }

            var diveSourcePtr = _builder.BuildInBoundsGEP2(sourceType, sourcePtr, new LLVMValueRef[] { _unit.CreateI32Constant(0), _unit.CreateI32Constant(0) });

            if (firstElementType.Kind == LLVMTypeKind.LLVMStructTypeKind)
            {
                return enterStructPointerForCoercedAccess(diveSourcePtr, firstElementType, destSize);
            }

            return (diveSourcePtr, firstElementType);
        }

        void buildAggStore(LLVMValueRef source, LLVMValueRef destPtr, bool lowAlignment)
        {
            if (source.TypeOf.Kind == LLVMTypeKind.LLVMStructTypeKind)
            {
                for (uint a = 0; a < source.TypeOf.StructElementTypesCount; a++)
                {
                    var elementPtr = _builder.BuildInBoundsGEP2(source.TypeOf, destPtr, new LLVMValueRef[] { _unit.CreateI32Constant(0), _unit.CreateI32Constant(a) });
                    var element = _builder.BuildExtractValue(source, a);
                    var storeInst = _builder.BuildStore(element, elementPtr);
                    if (lowAlignment)
                    {
                        storeInst.Alignment = 1;
                    }
                }
            }
            else
            {
                var storeInst = _builder.BuildStore(source, destPtr);
                if (lowAlignment)
                {
                    storeInst.Alignment = 1;
                }
            }
        }

        public (LLVMValueRef value, LLVMValueRef storage) decodeReturnValue(LLVMValueRef[] encodedArguments, LLVMValueRef returnValue, LLVMTypeRef returnType)
        {
            var returnArgInfo = _mapping.ReturnArgInfo;
            switch (returnArgInfo.getKind())
            {
                case ArgInfo.EArgKind.Ignore:
                    return (returnValue, null);

                case ArgInfo.EArgKind.Indirect:
                    {
                        var returnValuePointer = encodedArguments[_mapping.StructRetArgIndex];
                        var loadInst = _builder.BuildLoad2(returnType, returnValuePointer);
                        if (returnArgInfo.IndirectAlign > 0)
                        {
                            loadInst.Alignment = returnArgInfo.IndirectAlign;
                        }
                        return (loadInst, returnValuePointer);
                    }

                case ArgInfo.EArgKind.Cast:
                    throw new System.NotImplementedException("TODO CAST");
                case ArgInfo.EArgKind.Extend:
                case ArgInfo.EArgKind.Direct:
                    {
                        var coerceType = returnArgInfo.CoerceType;
                        var returnLLVMType = returnType;
                        var coerceLLVMType = coerceType;
                        LLVMValueRef destPtr = default;
                        LLVMValueRef loadInst = default;

                        if (LLVMHelper.typesAreEqual(returnLLVMType, coerceLLVMType) && returnArgInfo.DirectOffset == 0)
                        {
                            if (returnType.Kind == LLVMTypeKind.LLVMArrayTypeKind || returnType.Kind == LLVMTypeKind.LLVMStructTypeKind)
                            {
                                destPtr = createMemTemp(returnType);

                                buildAggStore(returnValue, destPtr, false);

                                loadInst = _builder.BuildLoad2(returnType, destPtr);
                                loadInst.Alignment = getTypeRequiredAlign(returnType);
                                return (loadInst, destPtr);
                            }
                            else
                            {
                                var castReturnValue = returnValue;
                                if (!LLVMHelper.typesAreEqual(returnValue.TypeOf, returnType))
                                {
                                    castReturnValue = _builder.BuildBitCast(returnValue, returnType);
                                }
                                return (castReturnValue, null);
                            }
                        }

                        destPtr = createMemTemp(coerceType);
                        var destType = returnType;

                        var storePtr = destPtr;

                        if (returnArgInfo.DirectOffset != 0)
                        {
                            storePtr = _builder.BuildBitCast(destPtr, LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0));
                            storePtr = _builder.BuildInBoundsGEP2(coerceType, storePtr, new LLVMValueRef[] { _unit.CreateI32Constant(returnArgInfo.DirectOffset) });
                            storePtr = _builder.BuildBitCast(storePtr, LLVMTypeRef.CreatePointer(coerceType, 0));
                            destType = coerceType;
                        }

                        createCoercedStore(returnValue, storePtr, coerceType, destType);

                        loadInst = _builder.BuildLoad2(returnType, destPtr);
                        loadInst.Alignment = getTypeRequiredAlign(returnType);
                        return (loadInst, destPtr);
                    }
                default:
                    throw new System.NotImplementedException("TODO");
            }
        }

        uint getTypeRequiredAlign(LLVMTypeRef type)
        {
            return _targetABI.getTypeRequiredAlign(_unit, type);
        }

        void createCoercedStore(LLVMValueRef source, LLVMValueRef destPtr, LLVMTypeRef sourceType, LLVMTypeRef destType)
        {
            if (LLVMHelper.typesAreEqual(sourceType, destType))
            {
                _builder.BuildStore(source, destPtr);
                return;
            }

            var sourceSize = _unit.Module.GetDataLayout().GetTypeAllocSize(sourceType);
            if (destType.Kind == LLVMTypeKind.LLVMStructTypeKind)
            {
                (destPtr, destType) = enterStructPointerForCoercedAccess(destPtr, destType, sourceSize);
            }

            if ((sourceType.Kind == LLVMTypeKind.LLVMIntegerTypeKind || sourceType.Kind == LLVMTypeKind.LLVMPointerTypeKind) &&
                (destType.Kind == LLVMTypeKind.LLVMIntegerTypeKind || destType.Kind == LLVMTypeKind.LLVMPointerTypeKind))
            {
                throw new System.NotImplementedException("TODO");
                /*
                var coeercedSource = coerceIntOrPtrToIntOrPtr(source, sourceType, destType);
                _builder.BuildStore(source, destPtr);
                return;*/
            }

            var destSize = _unit.Module.GetDataLayout().GetTypeAllocSize(destType);

            if (sourceSize <= destSize)
            {
                var sourcePtrType = LLVMTypeRef.CreatePointer(sourceType, 0);
                var casedDestPtr = _builder.BuildBitCast(destPtr, sourcePtrType);

                buildAggStore(source, casedDestPtr, true);
            }
            else
            {
                throw new System.NotImplementedException("TODO");
                /*
        const auto tempAlloca = createTempAlloca(typeInfo,
                                                     builder,
                                                     sourceType,
                                                     "coerce.mem.store");
            createStore(builder.getBuilder(), source, tempAlloca);
            const auto i8PtrType = builder.getBuilder().getInt8PtrTy();
            const auto casted = builder.getBuilder().CreateBitCast(tempAlloca, i8PtrType);
            const auto destCasted = builder.getBuilder().CreateBitCast(destPtr, i8PtrType);
            // FIXME: Use better alignment.
            builder.getBuilder().CreateMemCpy(destCasted, //dstAlign=//1,
                                              casted, //srcAlign=//1,
                                              llvm::ConstantInt::get(typeInfo.getLLVMType(IntPtrTy),
                                                                     destSize.asBytes()));
            */

            }
        }

    }

    public static class LLVMHelper
    {
        public static bool typesAreEqual(LLVMTypeRef type1, LLVMTypeRef type2)
        {
            if (type1.Kind == type2.Kind)
            {
                switch (type1.Kind)
                {
                    case LLVMTypeKind.LLVMFloatTypeKind:
                    case LLVMTypeKind.LLVMDoubleTypeKind:
                    case LLVMTypeKind.LLVMVoidTypeKind:
                        return true;
                    case LLVMTypeKind.LLVMIntegerTypeKind:
                        return type1.IntWidth == type2.IntWidth;
                    case LLVMTypeKind.LLVMPointerTypeKind:
                        return typesAreEqual(type1.ElementType, type2.ElementType);
                    case LLVMTypeKind.LLVMArrayTypeKind:
                        return type1.ArrayLength == type2.ArrayLength && typesAreEqual(type1.ElementType, type2.ElementType);
                    case LLVMTypeKind.LLVMVectorTypeKind:
                        return type1.VectorSize == type2.VectorSize && typesAreEqual(type1.ElementType, type2.ElementType);
                    case LLVMTypeKind.LLVMStructTypeKind:
                        if (type1.StructElementTypesCount != type2.StructElementTypesCount)
                        {
                            return false;
                        }
                        var structElementTypes1 = type1.GetStructElementTypes();
                        var structElementTypes2 = type2.GetStructElementTypes();
                        for (int a = 0; a < type1.StructElementTypesCount; a++)
                        {
                            if (!typesAreEqual(structElementTypes1[a], structElementTypes2[a]))
                            {
                                return false;
                            }
                        }
                        return true;
                    default:
                        throw new System.NotImplementedException("TODO");
                }
            }
            return false;
        }

        public static uint roundUpToPower2(uint size)
        {
            if (size == 0)
            {
                return 1;
            }
            size--;
            size |= size >> 1;
            size |= size >> 2;
            size |= size >> 4;
            size |= size >> 8;
            size |= size >> 16;
            size++;
            return size;
        }

        public static uint roundUpToAlign(uint size, uint align)
        {
            return (size + align - 1) & ~(align - 1);
        }
    }



}
