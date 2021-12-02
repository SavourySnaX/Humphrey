using Humphrey.Backend;
using Humphrey.FrontEnd;

namespace Humphrey
{
    public class CommonSymbolTableEntry
    {
        private IType _astType;
        private SemanticPass.SemanticInfo _semanticInfo;

        private CompilationType _compilationType;
        private CompilationFunction _compilationFunction;
        private CompilationValue _compilationValue;

        public CommonSymbolTableEntry(IType type, SemanticPass.SemanticInfo semanticInfo)
        {
            _astType = type;
            _semanticInfo = semanticInfo;
            _compilationType = null;
            _compilationFunction = null;
            _compilationValue = null;
        }

        public void SetCommpilationValue(CompilationValue value)
        {
            _compilationValue = value;
        }
        public void SetCommpilationType(CompilationType type)
        {
            _compilationType = type;
        }

        public void SetCommpilationFunction(CompilationFunction function)
        {
            _compilationFunction = function;
        }


        public IType AstType => _astType;
        public SemanticPass.SemanticInfo SemanticInfo => _semanticInfo;

        public CompilationType Type => _compilationType;
        public CompilationValue Value => _compilationValue;
        public CompilationFunction Function => _compilationFunction;
    }

}