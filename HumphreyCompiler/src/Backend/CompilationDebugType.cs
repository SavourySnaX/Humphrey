using LLVMSharp.Interop;

namespace Humphrey.Backend
{
    public class CompilationDebugType
    {
        string _debugIdentifier;
        LLVMMetadataRef _backendDebugType;

        public CompilationDebugType(string debugIdentifier, LLVMMetadataRef type)
        {
            _debugIdentifier = debugIdentifier;
            _backendDebugType = type;
        }

        public LLVMMetadataRef BackendType => _backendDebugType;
        public string Identifier => _debugIdentifier;
    }
}