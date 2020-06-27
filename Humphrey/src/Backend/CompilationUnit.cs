using LLVMSharp.Interop;
using static Extensions.Helpers;

using Humphrey.FrontEnd;
using System.Numerics;

namespace Humphrey.Backend
{
    public class CompilationUnit
    {
        LLVMContextRef contextRef;
        LLVMModuleRef moduleRef;
        public CompilationUnit(string name)
        {
            contextRef = CreateContext();
            moduleRef = contextRef.CreateModuleWithName(name);
        }

        public unsafe void Dump()
        {
            LLVM.DumpModule(moduleRef);
        }
        public CompilationType FetchIntegerType(uint numBits)
        {
            return new CompilationType(contextRef.GetIntType(numBits));
        }

        public CompilationFunctionType CreateFunctionType(CompilationParam[] inputs, CompilationParam[] outputs)
        {
            var allParameters = new LLVMTypeRef[inputs.Length + outputs.Length];
            var paramIdx = 0;
            foreach(var i in inputs)
            {
                allParameters[paramIdx++] = i.Type.BackendType;
            }
            foreach(var o in outputs)
            {
                //outputs need to be considered to be by ref
                allParameters[paramIdx++] = o.Type.AsPointer().BackendType;
            }
            return new CompilationFunctionType(Extensions.Helpers.CreateFunctionType(contextRef.VoidType, allParameters, false), inputs.Length);
        }

        public CompilationBuilder CreateBuilder(CompilationBlock bb)
        {
            var builder = contextRef.CreateBuilder();
            builder.PositionAtEnd(bb.BackendValue);
            return new CompilationBuilder(builder);
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
            var func = moduleRef.AddFunction(identifier, type.BackendType);

            return new CompilationFunction(func, type.OutParamOffset);
        }

        public CompilationParam CreateFunctionParameter(CompilationType type, string identifier)
        {
            return new CompilationParam(type, identifier);
        }
    }
}