using LLVMSharp.Interop;

namespace Humphrey.Backend
{
    public class CompilationFunction
    {
        LLVMValueRef function;
        int offsetToFirstOutParam;
        public CompilationFunction(LLVMValueRef func, int outParamOffset)
        {
            function = func;
            offsetToFirstOutParam = outParamOffset;
        }

        public LLVMValueRef BackendValue => function;

        public int OutParamOffset => offsetToFirstOutParam;
    }
}