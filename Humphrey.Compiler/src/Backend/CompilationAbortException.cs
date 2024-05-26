namespace Humphrey.Backend
{
    [System.Serializable]
    public class CompilationAbortException : System.Exception
    {
        public CompilationAbortException() { }
        public CompilationAbortException(string message) : base(message) { }
    }
}