using LLVMSharp.Interop;

using System.Collections.Generic;

namespace Humphrey.Backend
{
    public class CompilationFunction
    {
        LLVMValueRef function;
        CompilationFunctionType type;
        CompilationBlock exitBlock;

        HashSet<string> usedOutputs;
        public CompilationFunction(LLVMValueRef func, CompilationFunctionType funcType)
        {
            function = func;
            type = funcType;

            usedOutputs = new HashSet<string>();
        }

        public void MarkUsed(string identifier)
        {
            if (!usedOutputs.Contains(identifier))
                usedOutputs.Add(identifier);
        }

        public bool AreOutputsAllUsed()
        {
            if (!type.HasOutputs)
                return true;

            var param = type.Parameters;
            for (uint a = type.OutParamOffset; a < param.Length; a++)
            {
                if (!usedOutputs.Contains(param[a].Identifier))
                    return false;
            }

            return true;
        }

        public IEnumerable<CompilationParam> FetchMissingOutputs()
        {
            if (type.HasOutputs)
            {
                var param = type.Parameters;
                for (uint a = type.OutParamOffset; a < param.Length; a++)
                {
                    if (!usedOutputs.Contains(param[a].Identifier))
                        yield return param[a];
                }
            }
        }

        public LLVMValueRef BackendValue => function;
        public CompilationFunctionType FunctionType => type;
        public uint OutParamOffset => type.OutParamOffset;

        public CompilationBlock ExitBlock 
        {
             get
             {
                 return exitBlock;
             }
             set
             {
                 exitBlock=value;
             }
        }
    }
}