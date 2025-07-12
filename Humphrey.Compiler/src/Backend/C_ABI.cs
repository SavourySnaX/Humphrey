using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using Extensions;
using Humphrey.Backend;
using Humphrey.Compiler.src.Backend.ABI;
using LLVMSharp;
using LLVMSharp.Interop;
using Microsoft.VisualBasic;
using static Extensions.Helpers;


public class SystemV_C_ABI : CABI
{
    SystemV_C_ABI_STATIC.Classifier classifier;

    public List<ArgInfo> ComputeTransform(CompilationUnit unit, CompilationFunctionType functionType)
    {
        classifier = new SystemV_C_ABI_STATIC.Classifier(unit.Module.GetDataLayout());
        return classifier.classifyFunctionType(unit, functionType);
    }

    public uint getTypeRequiredAlign(CompilationUnit unit, LLVMTypeRef type)
    {
        return unit.Module.GetDataLayout().GetABIAlignmentOfType(type);
    }
}

public enum EClassification
{
    Unknown,
    Integer,
    SSE,
    SSEUp,
    X87,
    X87Up,
    ComplexX87,
    NoClass,
    Memory,
}

public static class SystemV_C_ABI_STATIC
{
    public static EClassification MergeClassification(EClassification first, EClassification second)
    {
        if (first==second)
        {
            return first;
        }

        if (first == EClassification.NoClass)
        {
            return second;
        }

        if (second == EClassification.NoClass)
        {
            return first;
        }

        if (first == EClassification.Memory || second == EClassification.Memory)
        {
            return EClassification.Memory;
        }

        if (first == EClassification.Integer || second == EClassification.Integer)
        {
            return EClassification.Integer;
        }

        if (first == EClassification.X87 || first == EClassification.X87Up || first == EClassification.ComplexX87
            || second == EClassification.X87 || second == EClassification.X87Up || second == EClassification.ComplexX87)
        {
            return EClassification.Memory;
        }

        return EClassification.SSE;
    }

    public class Classification
    {
        EClassification [] classification;
        LLVMTargetDataRef dataLayout;

        public Classification(LLVMTargetDataRef dataLayout)
        {
            this.dataLayout = dataLayout;
            classification = new EClassification[2];
            classification[0] = EClassification.NoClass;
            classification[1] = EClassification.NoClass;
        }

        public EClassification Low => classification[0];
        public EClassification High => classification[1];
        public bool IsMemory => classification[0] == EClassification.Memory;

        public void AddField(int offset, EClassification classification)
        {
            if (IsMemory)
            {
                return;
            }

            var idx = offset < 8 ? 0 : 1;

            var merged = MergeClassification(this.classification[idx], classification);

            if (merged != this.classification[idx])
            {
                this.classification[idx] = merged;
                if (merged == EClassification.Memory)
                {
                    this.classification[1-idx] = EClassification.Memory;
                }
            }
        }

        public void classifyType(CompilationType type, int offset, bool isNamedArg)
        {
            switch (type.BackendType.Kind)
            {
                case LLVMTypeKind.LLVMVoidTypeKind:
                    AddField(offset, EClassification.NoClass);
                    return;
                case LLVMTypeKind.LLVMPointerTypeKind:
                case LLVMTypeKind.LLVMIntegerTypeKind:
                    AddField(offset, EClassification.Integer);
                    return;
                case LLVMTypeKind.LLVMFloatTypeKind:
                case LLVMTypeKind.LLVMDoubleTypeKind:
                     AddField(offset, EClassification.SSE);
                    return;
                case LLVMTypeKind.LLVMStructTypeKind:
                    {
                        var members = (type as CompilationStructureType).Elements;

                        UInt64 structOffset=0;
                        for (uint a=0;a<members.Length;a++)
                        {
                            var member = members[a];
                            var memOffset = dataLayout.GetOffsetOfElement(type.BackendType, a);
                            if (memOffset < structOffset)
                            {
                                throw new Exception("TODO");
                            }
                            else
                            {
                                structOffset = memOffset;
                            }

                            classifyType(member, (int)((uint)offset + structOffset), isNamedArg);

                            structOffset += dataLayout.GetTypeAllocSize(member.BackendType);
                        }
                    }
                    return;
                default:
                    throw new System.Exception($"TODO classify Type - Unsupported type {type.DumpType()}");
            }
        }

    }

    public class Classifier
    {
        LLVMTargetDataRef dataLayout;

        public Classifier(LLVMTargetDataRef dataLayout)
        {
            this.dataLayout = dataLayout;
        }

        Classification classify(CompilationType type, bool isNamedArg)
        {
            var result = new Classification(dataLayout);
            if ((dataLayout.GetTypeAllocSize(type.BackendType) > 32) || dataLayout.HasUnalignedFields(type.BackendType))
            {
                result.AddField(0, EClassification.Memory);
                return result;
            }

            result.classifyType(type, 0, isNamedArg);

            return result;
        }

        public List<ArgInfo> classifyFunctionType(CompilationUnit unit, CompilationFunctionType functionType)
        {
            ArgInfo returnInfo = default;
            if (functionType.ReturnType != null)
            {
                returnInfo = classifyReturnType(unit, functionType.ReturnType.Type);
            }

            var argInfoArray = new List<ArgInfo>();
            argInfoArray.Add(returnInfo);

            uint freeIntRegs = 6;
            uint freeSSERegs = 8;

            if (returnInfo.IsDirect)
            {
                freeIntRegs--;
            }

            var numRequiredArgs = functionType.Parameters.Length;

            for (int a=0;a<numRequiredArgs;a++)
            {
                bool isNamedArg = a < numRequiredArgs;
                var argType = functionType.Parameters[a].Type;
                bool isArgument=true;
                uint neededInt =0;
                uint neededSSE = 0;
                var argInfo = classifyType(unit, argType, isArgument, freeIntRegs, ref neededInt, ref neededSSE, isNamedArg);

                if (freeIntRegs>=neededInt && freeSSERegs>=neededSSE)
                {
                    freeIntRegs -= neededInt;
                    freeSSERegs -= neededSSE;
                }
                else
                {
                    throw new Exception("TODO");

                    //argInfo = ArgInfo.getIndirectResult(unit, argType, freeIntRegs);
                }

                argInfoArray.Add(argInfo);
            }

            return argInfoArray;
        }

        public ArgInfo classifyReturnType(CompilationUnit unit, CompilationType type)
        {
            uint neededInt = 0;
            uint neededSSE = 0;
            return classifyType(unit, type,false,0,ref neededInt,ref neededSSE, true);
        }

        public bool bitsContainNoUserData(LLVMTargetDataRef dataLayout, LLVMTypeRef type, ulong start, ulong end)
        {
            var size = dataLayout.GetTypeAllocSize(type);
            if (size * 8 <= start)
            {
                return true;
            }

            if (type.Kind == LLVMTypeKind.LLVMStructTypeKind)
            {
                for (uint a=0;a<type.StructElementTypesCount;a++)
                {
                    var structElementTypes = type.GetStructElementTypes();
                    var member = structElementTypes[a];
                    var offset = dataLayout.GetOffsetOfElement(type, a);

                    if (offset*8 >= end)
                    {
                        return true;
                    }

                    ulong fieldStart = (offset*8<start) ? start-offset*8 : 0;
                    if (!bitsContainNoUserData(dataLayout, member, fieldStart, end-offset*8))
                    {
                        return false;
                    }
                }

                return true;
            }

            if (type.Kind == LLVMTypeKind.LLVMArrayTypeKind)
            {
                throw new Exception("TODO");
            }

            return false;
        }

		bool containsFloatAtOffset(LLVMTargetDataRef dataLayout, LLVMTypeRef type, ulong offset)
        {
			// Base case if we find a float.
			if (offset == 0 && type.Kind==LLVMTypeKind.LLVMFloatTypeKind) 
            {
				return true;
			}
			
			// If this is a struct, recurse into the field at the specified offset.
			if (type.Kind == LLVMTypeKind.LLVMStructTypeKind)
            {
                // Find the field containing the offset.
                for (int a=0;a<type.StructElementTypesCount;a++)
                {
                    var structElementTypes = type.GetStructElementTypes();
                    var member = structElementTypes[a];
                    var memberOffset = dataLayout.GetOffsetOfElement(type, (uint)a);
                    var memberSize = dataLayout.GetTypeAllocSize(member);
                    if (memberOffset <= offset && offset < memberOffset + memberSize)
                    {
                        return containsFloatAtOffset(dataLayout, member, offset - memberOffset);
                    }
                }
			}
			
            if (type.Kind == LLVMTypeKind.LLVMArrayTypeKind)
            {
                throw new Exception("TODO");
                /*
			// If this is an array, recurse into the field at the specified offset.
			if (type.isArray()) {
				const auto elementType = type.arrayElementType();
				const auto elementSize = typeInfo.getTypeAllocSize(elementType);
				const auto elementOffset = elementSize * (offset / elementSize);
				assert(elementOffset <= offset);
				const auto relativeOffset = offset - elementOffset;
				return containsFloatAtOffset(typeInfo,
				                             elementType,
				                             relativeOffset);
                                             */
			}
			
			return false;
		}

        LLVMTypeRef getSSETypeAtOffset(CompilationUnit unit, CompilationType sourceType, uint sourceOffset, CompilationType type, uint offset)
        {
			// The only three choices we have are either double,
			// <2 x float>, or float.
            var dataLayout = unit.Module.GetDataLayout();
			if (bitsContainNoUserData(dataLayout,type.BackendType, sourceOffset*8+32, sourceOffset*8+64))
            {
                // We pass as float if the last 4 bytes is just
                // padding.  This happens for structs that
                // contain 3 floats.
                return unit.Context.FloatType;
			}
			
			// We want to pass as <2 x float> if the LLVM IR type
			// contains a float at offset+0 and offset+4.  Walk
			// the type to find out if this is the case.
			if (containsFloatAtOffset(dataLayout, type.BackendType, offset) &&
			    containsFloatAtOffset(dataLayout, type.BackendType, offset + 4))
            {
                return CreateVectorType(unit.Context.FloatType, 2);
			}
			
			return unit.Context.DoubleType;
		}

        LLVMTypeRef getIntegerTypeAtOffset(CompilationUnit unit, LLVMTypeRef type, uint offset, LLVMTypeRef sourceType, uint sourceOffset)
        {
            ulong typeSize = default;
            if (sourceOffset!=0 && sourceOffset!=8)
            {
                throw new Exception("Invalid source offset");
            }

            if (offset == 0)
            {
                typeSize = unit.Module.GetDataLayout().GetTypeAllocSize(type);

                if (type.Kind == LLVMTypeKind.LLVMIntegerTypeKind || type.Kind == LLVMTypeKind.LLVMPointerTypeKind)
                {
                    if (typeSize == 8)
                    {
                        return type;
                    }
                    else if ( typeSize == 4 || typeSize == 2 || typeSize == 1)
                    {
                        if (bitsContainNoUserData(unit.Module.GetDataLayout(), sourceType, sourceOffset*8+typeSize*8, sourceOffset*8+64))
                        {
                            return type;
                        }
                    }
                }
            }

            if (type.Kind == LLVMTypeKind.LLVMStructTypeKind && offset < unit.Module.GetDataLayout().GetTypeAllocSize(type))
            {
                for (int a=0;a<type.StructElementTypesCount;a++)
                {
                    var structElementTypes = type.GetStructElementTypes();
                    var member = structElementTypes[a];
                    var memberOffset = unit.Module.GetDataLayout().GetOffsetOfElement(type, (uint)a);
                    var memberSize = unit.Module.GetDataLayout().GetTypeAllocSize(member);
                    if (memberOffset <= offset && offset < memberOffset + memberSize)
                    {
                        return getIntegerTypeAtOffset(unit, member, (uint)(offset - memberOffset), sourceType, sourceOffset);
                    }
                }
            }

            if (type.Kind == LLVMTypeKind.LLVMArrayTypeKind)
            {
                throw new Exception("TODO");
                /*
                if (offset < unit.Module.GetDataLayout().GetTypeAllocSize(type))
                {
                    const auto elementType = type.arrayElementType();
                    const auto elementSize = typeInfo.getTypeAllocSize(elementType);
                    const auto elementOffset = elementSize * (offset / elementSize);
                    assert(elementOffset <= offset);
                    const auto relativeOffset = offset - elementOffset;
                    return getIntegerTypeAtOffset(unit,
                                                  elementType,
                                                  relativeOffset,
                                                  sourceType,
                                                  sourceOffset);
                }*/
            }

            typeSize = unit.Module.GetDataLayout().GetTypeAllocSize(sourceType);
            if (typeSize!=sourceOffset)
            {
                throw new Exception("Empty field?");
            }

            var intSize = typeSize - sourceOffset > 8 ? 8 : typeSize - sourceOffset;
            return unit.CreateIntegerType(((uint)intSize) * 8, false, new SourceLocation()).BackendType;
        }

        ArgInfo getIndirectReturnResult(CompilationUnit unit, CompilationType type)
        {
            if (!type.BackendType.IsAggregateType())
            {
                return type.BackendType.IsPromotableIntegerType() ? ArgInfo.getExtend(unit, type.BackendType) : ArgInfo.getDirect(unit, type.BackendType);
            }
            return ArgInfo.getIndirect(unit, 0);
        }

        LLVMTypeRef getX86_64ByValArgumentPair(CompilationUnit unit, LLVMTypeRef lowType,LLVMTypeRef highType) 
        {
            // In order to correctly satisfy the ABI, we need the
            // high part to start at offset 8.  If the high and low
            // parts we inferred are both 4-byte types (e.g. i32 and
            // i32) then the resultant struct type ({i32,i32}) won't
            // have the second element at offset 8.  Check for this:
            var lowSize = (uint)unit.Module.GetDataLayout().GetTypeAllocSize(lowType);
            var highAlign = getTypeRequiredAlign(unit, highType);
			var highStart = roundUpToAlign(lowSize, highAlign);

            if (highStart == 0 || highStart>8)
            {
                throw new Exception("Invalid x86-64 argument pair!");
            }
			
			// To handle this, we have to increase the size of the
			// low part so that the second element will start at an
			// 8 byte offset.  We can't increase the size of the
			// second element because it might make us access off the
			// end of the struct.
            if (highStart!=8)
            {
				// There are only two sorts of types the ABI
				// generation code can produce for the low part
				// of a pair that aren't 8 bytes in size: float
				// or i8/i16/i32. Promote these to a larger type.
                if (lowType.Kind == LLVMTypeKind.LLVMFloatTypeKind)
                {
                    lowType = unit.Context.DoubleType;
                }
                else if (lowType.Kind == LLVMTypeKind.LLVMIntegerTypeKind)
                {
                    lowType = unit.Context.Int64Type;
                }
                else
                {
                    throw new Exception("Invalid/unknown low type.");
                }
			}


            var types = new LLVMTypeRef[2] { lowType, highType };
            var resultType = unit.Context.GetStructType(types, true);


            if (unit.Module.GetDataLayout().GetOffsetOfElement(resultType, 1) != 8)
            {
                throw new Exception("Invalid x86-64 argument pair!");
            }
			
			return resultType;
		}

        public ArgInfo classifyType(CompilationUnit unit, CompilationType type, bool isArgument, uint freeIntRegs, ref uint neededInt, ref uint neededSSE, bool isNamedArg)
        {
            var classification = classify(type, isNamedArg);

            neededInt=0;
            neededSSE=0;

            var resultType = unit.Context.VoidType;
            switch (classification.Low)
            {
                case EClassification.NoClass:
                    if (classification.High == EClassification.NoClass)
                    {
                        return ArgInfo.getIgnore();
                    }
                    break;
                case EClassification.SSEUp:
                    throw new System.Exception("SSEUp not possible in low word");
                case EClassification.X87Up:
                    throw new System.Exception("X87Up not possible in low word");
                case EClassification.Memory:
                    if (isArgument)
                    {
                        throw new Exception("TODO");
                        //return getIndirectResult(type, freeIntRegs);
                    }
                    else
                    {
                        return getIndirectReturnResult(unit, type);
                    }
                case EClassification.Integer:
                    neededInt++;
                    resultType = getIntegerTypeAtOffset(unit, type.BackendType, 0, type.BackendType, 0);
                    if (classification.High == EClassification.NoClass && resultType.Kind == LLVMTypeKind.LLVMIntegerTypeKind)
                    {
                        if (type.BackendType.IsIntegralType() && type.BackendType.IsPromotableIntegerType())
                        {
                            return ArgInfo.getExtend(unit, resultType);
                        }
                    }
                    break;
                case EClassification.SSE:
                    neededSSE++;
                    resultType = getSSETypeAtOffset(unit, type, 0, type, 0);
                    break;
                case EClassification.X87:
                    throw new Exception("TODO");
                    /*
                    if (isArgument)
                    {
                        return getIndirectResult(type, freeIntRegs);
                    }
                    else
                    {
                        resultType = unit.Context.X86FP80Type;
                    }
                    break;*/
                case EClassification.ComplexX87:
                    throw new Exception("TODO");
                    /*
                    if (isArgument)
                    {
                        return getIndirectResult(type, freeIntRegs);
                    }
                    else
                    {
                        throw new Exception("TODO Create {FP80,FP80} struct type");
                        //resultType = 
                    }
                    break;*/
            }

            var highPartType = unit.Context.VoidType;

            switch (classification.High)
            {
                case EClassification.Memory:
                case EClassification.X87:
                case EClassification.ComplexX87:
                    throw new Exception("Unreachable - cant have high part as memory or x87");
                case EClassification.NoClass:
                    break;
                case EClassification.Integer:
                    neededInt++;
                    highPartType = getIntegerTypeAtOffset(unit, type.BackendType, 8, type.BackendType, 8);
                    if (classification.Low == EClassification.NoClass)
                    {
                        return ArgInfo.getDirect(unit, highPartType, 8);
                    }
                    break;
                case EClassification.SSE:
                    highPartType = getSSETypeAtOffset(unit, type, 8, type, 8);
                    if (classification.Low == EClassification.NoClass)
                    {
                        return ArgInfo.getDirect(unit, highPartType, 8);
                    }
                    neededSSE++;
                    break;
                case EClassification.SSEUp:
                    throw new Exception("TODO");
                    /*resultType = getByteVectorType(type);
                    break;*/
                case EClassification.X87Up:
                    throw new Exception("TODO");
                    /*
                    if (classification.Low != EClassification.X87)
                    {
                        highPartType = getSSETypeAtOffset(unit, type, 8, type, 8);
                        if (classification.Low == EClassification.NoClass)
                        {
                            return ArgInfo.getDirect(unit, highPartType, 8);
                        }
                    }
                    neededSSE++;
                    break;*/
            }

            if (highPartType.Kind != LLVMTypeKind.LLVMVoidTypeKind)
            {
                resultType = getX86_64ByValArgumentPair(unit, resultType, highPartType);
            }

            if (typesAreEqual(resultType,type.BackendType))
            {
                resultType = type.BackendType;
            }

            return ArgInfo.getDirect(unit, resultType);
        }
    }

/*
    public static List<Classification> ClassifyType(LLVMModuleRef module, CompilationStructureType type)
    {
        if (type.BackendType.Kind == LLVMTypeKind.LLVMVoidTypeKind)



        var dataLayout = module.GetDataLayout();
        var size = dataLayout.GetABISizeOfType(type.BackendType);
        if (size >= 64)
        {
            return new List<Classification> { Classification.Memory };
        }
        // TODO - Check for Unaligned fields
        if (type.Fields.Length == 1)
        {
            if (type.Fields[0] is CompilationFloatType)
            {
                return new List<Classification> { Classification.SSE };
            }
            if (type.Fields[0] is CompilationIntegerType)
            {
                return new List<Classification> { Classification.Integer };
            }
            return new List<Classification> { Classification.Memory };
        }
        var fields = type.Fields;
        for (int a=0;a<fields.Length;a++)
        {
            var field = fields[a];
            if (field.Type is CompilationFloatType)
            {
                return new List<Classification> { Classification.SSE };
            }
        }
        foreach (var field in type.Fields)
        {
            if (field.Type is CompilationFloatType)
            {
                return new List<Classification> { Classification.SSE };
            }
        }


    }

    public static Classification ClassifyPair(CompilationType type1, CompilationType type2)
    {
        if (type1 is CompilationFloatType && type2 is CompilationFloatType)
        {
            return Classification.SSE;
        }
        throw new System.Exception($"Unsupported pair classification {type1.DumpType()} and {type2.DumpType()}");
    }*/

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

    public static uint getExpansionSize(LLVMTypeRef type) 
    {
        if (type.Kind == LLVMTypeKind.LLVMVoidTypeKind)
        {
            throw new Exception("Should not be called with void type");
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

	public static FunctionIRMapping getFunctionIRMapping(List<ArgInfo> argInfoArray) 
    {
		var functionIRMapping = new FunctionIRMapping();
		
		uint irArgumentNumber = 0;
		bool swapThisWithSRet = false;
		
		functionIRMapping.setReturnArgInfo(argInfoArray[0]);
		
		var returnArgInfo = functionIRMapping.ReturnArgInfo;
		
		if (returnArgInfo.getKind() == ArgInfo.EArgKind.Indirect) 
        {
			swapThisWithSRet = returnArgInfo.IsSRetAfterThis;
			functionIRMapping.setStructRetArgIndex(	swapThisWithSRet ? 1 : irArgumentNumber++);
		}

        for (int argumentNumber = 1; argumentNumber < argInfoArray.Count; argumentNumber++)
        {
			var argInfo = argInfoArray[argumentNumber];
			
			var argumentIRMapping= new ArgumentIRMapping();
			argumentIRMapping.argInfo = argInfo;
			
			if (argInfo.PaddingType.Kind !=  LLVMTypeKind.LLVMVoidTypeKind) 
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
					argumentIRMapping.numberOfIRArgs = 1;
					break;
				case ArgInfo.EArgKind.Ignore:
				case ArgInfo.EArgKind.InAlloca:
					// ignore and inalloca doesn't have matching LLVM parameters.
					argumentIRMapping.numberOfIRArgs = 0;
					break;
				case ArgInfo.EArgKind.Expand: {
					argumentIRMapping.numberOfIRArgs = getExpansionSize(argInfo.getExpandType());
					break;
				}
			}
			
			if (argumentIRMapping.numberOfIRArgs > 0) 
            {
				argumentIRMapping.firstArgIndex = irArgumentNumber;
				irArgumentNumber += argumentIRMapping.numberOfIRArgs;
			}
			
			// Skip over the sret parameter when it comes
			// second. We already handled it above.
			if (irArgumentNumber == 1 && swapThisWithSRet) 
            {
				irArgumentNumber++;
			}
			
			functionIRMapping.Arguments.Add(argumentIRMapping);
		}
		
// 		if (FI.usesInAlloca()) {
// 			functionIRMapping.setInallocaArgIndex(irArgumentNumber++);
// 		}
		
		functionIRMapping.setTotalIRArgs(irArgumentNumber);
		
		return functionIRMapping;
	}

    public static LLVMTypeRef getFunctionType(LLVMContextRef context, LLVMTypeRef returnType, LLVMTypeRef[] paramTypes ,FunctionIRMapping functionIRMapping) 
    {
        LLVMTypeRef resultType = default;
		
        var returnArgInfo = functionIRMapping.ReturnArgInfo;
		switch (returnArgInfo.getKind()) 
        {
			case ArgInfo.EArgKind.Expand:
                throw new Exception("Invalid ABI kind for return argument");
			
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
                        resultType =  context.VoidType;
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
			throw new Exception("TODO");
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
                    if (numIRArgs!=0)
                    {
                        throw new Exception("Invalid number of IR args for Ignore or InAlloca");
                    }
					break;
				
				case ArgInfo.EArgKind.Indirect:
                    {
                        if (numIRArgs != 1)
                        {
                            throw new Exception("Invalid number of IR args for Indirect");
                        }
                        // Indirect arguments are always on
                        // the stack, which is addr space #0.
                        argumentTypes[firstIRArg] = Extensions.Helpers.CreatePointerType(argumentType);
                        break;
                    }

                case ArgInfo.EArgKind.Extend:
				case ArgInfo.EArgKind.Direct: 
                {
					// Fast-isel and the optimizer generally like scalar values better than
					// FCAs, so we flatten them if this is safe to do for this argument.
					var coerceType = argInfo.CoerceType;
					if (coerceType.Kind==LLVMTypeKind.LLVMStructTypeKind && argInfo.IsDirect && argInfo.CanBeFlattened) 
                    {
                        if (numIRArgs != coerceType.StructElementTypesCount)
                        {
                            throw new Exception("Invalid number of IR args for Direct");
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
                            throw new Exception("Invalid number of IR args for Direct");
                        }
						argumentTypes[firstIRArg] = coerceType;
					}
					break;
				}

				case ArgInfo.EArgKind.Expand:
                    throw new Exception("TODO");
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

    static uint roundUpToPower2(uint size)
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

    static uint roundUpToAlign(uint size, uint align)
    {
        return (size + align - 1) & ~(align - 1);
    }

    static uint getTypeRequiredAlign(CompilationUnit unit, LLVMTypeRef type)
    {
        switch (type.Kind)
        {
            case LLVMTypeKind.LLVMVoidTypeKind:
                return 1;
            case LLVMTypeKind.LLVMPointerTypeKind:
                return 8;
            case LLVMTypeKind.LLVMIntegerTypeKind:
                return roundUpToPower2(type.IntWidth / 8);
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
                throw new Exception("TODO");
        }
    }

    static bool typesAreEqual(LLVMTypeRef type1, LLVMTypeRef type2)
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
                    throw new Exception("TODO");
            }
        }
        return false;
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

        public Caller(LLVMTypeRef functionType, LLVMValueRef function, LLVMValueRef[] backendValues, FunctionIRMapping mapping, LLVMBuilderRef builder, LLVMBuilderRef locals, CompilationUnit unit)
        {
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
            var result = _builder.CreateAlloca(_locals, type, "abi_tempAlloca");
            return result;
        }

        LLVMValueRef createMemTemp(LLVMTypeRef type)
        {
            var result = _builder.CreateAlloca(_locals, type, "abi_memTemp");
            result.Alignment = getTypeRequiredAlign(type);
            return result;
        }

        public List<LLVMValueRef> encodeArguments(CompilationFunctionType funcType)
        {
            var args = new LLVMValueRef[_mapping.TotalIRArgs];

            var returnArgInfo = _mapping.ReturnArgInfo;
            //LLVMValueRef argMemory = default;
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
                    throw new Exception("TODO");
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
                        throw new Exception("TODO");
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
                            throw new Exception("Invalid number of IR args for Indirect");
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
                            throw new Exception("TODO");
                        }
                        break;

                    case ArgInfo.EArgKind.Ignore:
                        if (numIRArgs!=0)
                        {
                            throw new Exception("Invalid number of IR args for Ignore");
                        }
                        break;

                    case ArgInfo.EArgKind.Extend:
                    case ArgInfo.EArgKind.Direct:
                        {
                            var coerceType = argInfo.CoerceType;

                            if ((coerceType.Kind == LLVMTypeKind.LLVMStructTypeKind) && (coerceType.Kind == argumentType.BackendType.Kind) && (argInfo.DirectOffset == 0))
                            {
                                if (numIRArgs!=1)
                                {
                                    throw new Exception("Invalid number of IR args for Direct");
                                }

                                var value = argumentValue;
                                var llvmArgType = argumentType.BackendType;

                                if (llvmArgType!=value.TypeOf)
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

                                if (sourceSize<destSize)
                                {
                                    var tempAlloca = createMemTemp(coerceType);
                                    //MEMCPY (tempAlloca, sourcePtr, sourceSize)
                                    sourcePtr=tempAlloca;
                                }
                                else
                                {
                                    sourcePtr = _builder.BuildBitCast(sourcePtr, LLVMTypeRef.CreatePointer(coerceType, 0));
                                }

                                if (numIRArgs != coerceType.StructElementTypesCount)
                                {
                                    throw new Exception("Invalid number of IR args for Direct");
                                }

                                for (int a=0;a<coerceType.StructElementTypesCount;a++)
                                {
                                    throw new Exception("TODO");
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
                                    throw new Exception("Invalid number of IR args for Direct");
                                }
                                args[firstIRArg] = createCoercedLoad(sourcePtr, argumentType.BackendType, coerceType);
                            }
                            break;
                        }
                    case ArgInfo.EArgKind.Expand:
                        {
                            var alloca = createMemTemp(argumentType.BackendType);
                            var storeInst = _builder.BuildStore(argumentValue, alloca);
                            throw new Exception("TODO");
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
            if (typesAreEqual(sourceType,destType))
            {
                return _builder.BuildLoad2(sourceType, sourcePtr);
            }

            var destSize = _unit.Module.GetDataLayout().GetTypeAllocSize(destType);

            if (sourceType.Kind == LLVMTypeKind.LLVMStructTypeKind)
            {
                (sourcePtr, sourceType) = enterStructPointerForCoercedAccess(sourcePtr, sourceType, destSize);
            }

            var sourceSize = _unit.Module.GetDataLayout().GetTypeAllocSize(sourceType);

            if ( (destType.Kind == LLVMTypeKind.LLVMIntegerTypeKind || destType.Kind==LLVMTypeKind.LLVMPointerTypeKind) &&
                 (sourceType.Kind == LLVMTypeKind.LLVMIntegerTypeKind || sourceType.Kind==LLVMTypeKind.LLVMPointerTypeKind))
            {
                throw new Exception("TODO");
                /*
                var loadInst = _builder.BuildLoad2(sourceType, sourcePtr);
                return coerceIntOrPtrToIntOrPtr(loadInst, sourceType, destType);*/
            }

            if (sourceSize>=destSize)
            {
                var asPointer = LLVMTypeRef.CreatePointer(destType, 0);
                var casted = _builder.BuildBitCast(sourcePtr, asPointer);
                var loadInst = _builder.BuildLoad2(destType, casted);
                loadInst.Alignment = 1;
                return loadInst;
            }
            else
            {
                throw new Exception("TODO");
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
            if (sourceType.StructElementTypesCount==0)
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

            var diveSourcePtr = _builder.BuildInBoundsGEP2(sourceType, sourcePtr,new LLVMValueRef[] { _unit.CreateI32Constant(0), _unit.CreateI32Constant(0) });

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
                for (uint a=0;a<source.TypeOf.StructElementTypesCount;a++)
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
                        if (returnArgInfo.IndirectAlign>0)
                        {
                            loadInst.Alignment = returnArgInfo.IndirectAlign;
                        }
                        return (loadInst, returnValuePointer);
                    }

                case ArgInfo.EArgKind.Extend:
                case ArgInfo.EArgKind.Direct:
                    {
                        var coerceType = returnArgInfo.CoerceType;
                        var returnLLVMType = returnType;
                        var coerceLLVMType = coerceType;
                        LLVMValueRef destPtr = default;
                        LLVMValueRef loadInst = default;

                        if (typesAreEqual(returnLLVMType,coerceLLVMType) && returnArgInfo.DirectOffset == 0)
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
                                if (!typesAreEqual(returnValue.TypeOf, returnType))
                                {
                                    castReturnValue = _builder.BuildBitCast(returnValue, returnType);
                                }
                                return (castReturnValue, null);
                            }
                        }

                        destPtr = createMemTemp(coerceType);
                        var destType = returnType;

                        var storePtr = destPtr;

                        if (returnArgInfo.DirectOffset!=0)
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
                    throw new Exception("TODO");
            }
        }

        uint getTypeRequiredAlign(LLVMTypeRef type)
        {
            return SystemV_C_ABI_STATIC.getTypeRequiredAlign(_unit, type);
        }

        void createCoercedStore(LLVMValueRef source, LLVMValueRef destPtr, LLVMTypeRef sourceType, LLVMTypeRef destType)
        {
            if (typesAreEqual(sourceType, destType))
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
                throw new Exception("TODO");
                /*
                var coeercedSource = coerceIntOrPtrToIntOrPtr(source, sourceType, destType);
                _builder.BuildStore(source, destPtr);
                return;*/
            }

            var destSize = _unit.Module.GetDataLayout().GetTypeAllocSize(destType);

            if (sourceSize<=destSize)
            {
                var sourcePtrType = LLVMTypeRef.CreatePointer(sourceType, 0);
                var casedDestPtr = _builder.BuildBitCast(destPtr, sourcePtrType);

                buildAggStore(source, casedDestPtr, true);
            }
            else
            {
                throw new Exception("TODO");
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

}