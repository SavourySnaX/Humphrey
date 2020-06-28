using LLVMSharp.Interop;

namespace Humphrey.Backend
{
    public class CompilationFunction
    {
        LLVMValueRef function;
        CompilationFunctionType type;
        public CompilationFunction(LLVMValueRef func, CompilationFunctionType funcType)
        {
            function = func;
            type = funcType;
        }

        public LLVMValueRef BackendValue => function;
        public CompilationFunctionType FunctionType => type;
        public uint OutParamOffset => type.OutParamOffset;
    }
}