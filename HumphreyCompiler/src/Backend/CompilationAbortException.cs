namespace Humphrey.Backend
{
    [System.Serializable]
    public class CompilationAbortException : System.Exception
    {
        public CompilationAbortException() { }
        public CompilationAbortException(string message) : base(message) { }
        public CompilationAbortException(string message, System.Exception inner) : base(message, inner) { }
        protected CompilationAbortException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}