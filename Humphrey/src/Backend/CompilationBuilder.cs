using LLVMSharp.Interop;

namespace Humphrey.Backend
{
    public class CompilationBuilder
    {
        LLVMBuilderRef builderRef;
        CompilationFunction function;

        public CompilationBuilder(LLVMBuilderRef builder, CompilationFunction func)
        {
            builderRef = builder;
            function = func;
        }

        public CompilationValue Load(CompilationValue value)
        {
            return new CompilationValue(builderRef.BuildLoad(value.BackendValue), value.Type);
        }

        public CompilationValue UDiv(CompilationValue left, CompilationValue right)
        {
            return new CompilationValue(builderRef.BuildUDiv(left.BackendValue, right.BackendValue), left.Type);
        }
        
        public CompilationValue SDiv(CompilationValue left, CompilationValue right)
        {
            return new CompilationValue(builderRef.BuildSDiv(left.BackendValue, right.BackendValue), left.Type);
        }

        public CompilationValue URem(CompilationValue left, CompilationValue right)
        {
            return new CompilationValue(builderRef.BuildURem(left.BackendValue, right.BackendValue), left.Type);
        }
        
        public CompilationValue SRem(CompilationValue left, CompilationValue right)
        {
            return new CompilationValue(builderRef.BuildSRem(left.BackendValue, right.BackendValue), left.Type);
        }

        public CompilationValue Mul(CompilationValue left, CompilationValue right)
        {
            return new CompilationValue(builderRef.BuildMul(left.BackendValue, right.BackendValue), left.Type);
        }
        
        public CompilationValue Add(CompilationValue left, CompilationValue right)
        {
            return new CompilationValue(builderRef.BuildAdd(left.BackendValue, right.BackendValue), left.Type);
        }

        public CompilationValue Sub(CompilationValue left, CompilationValue right)
        {
            return new CompilationValue(builderRef.BuildSub(left.BackendValue, right.BackendValue), left.Type);
        }
        public LLVMBuilderRef BackendValue => builderRef;
        public CompilationFunction Function => function;
    }
}
