using System.Collections.Generic;

namespace Humphrey.Backend
{
    public class SymbolTable
    {
        private readonly Dictionary<string, CompilationType> typeTable;
        private readonly Dictionary<string, CompilationFunction> functionTable;
        private readonly Dictionary<string, CompilationValue> valueTable;

        public SymbolTable()
        {
            typeTable = new Dictionary<string, CompilationType>();
            functionTable = new Dictionary<string, CompilationFunction>();
            valueTable = new Dictionary<string, CompilationValue>();
        }

        public CompilationType FetchType(string identifier)
        {
            if (typeTable.TryGetValue(identifier,out var result))
                return result;
            return null;
        }

        public bool TypeDefined(string identifier)
        {
            return typeTable.ContainsKey(identifier);
        }

        public void AddType(string identifier, CompilationType type)
        {
            typeTable.Add(identifier, type);
        }

        public CompilationFunction FetchFunction(string identifier)
        {
            if (functionTable.TryGetValue(identifier,out var result))
                return result;
            return null;
        }

        public bool AddFunction(string identifier, CompilationFunction function)
        {
            if (functionTable.ContainsKey(identifier))
                return false;
            functionTable.Add(identifier, function);
            return true;
        }

        public CompilationValue FetchValue(string identifier)
        {
            if (valueTable.TryGetValue(identifier,out var result))
                return result;
            return null;
        }

        public bool AddValue(string identifier, CompilationValue value)
        {
            if (valueTable.ContainsKey(identifier))
                return false;
            valueTable.Add(identifier, value);
            return true;
        }

    }
}