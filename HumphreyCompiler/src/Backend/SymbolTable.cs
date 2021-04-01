using System.Collections.Generic;
using Humphrey.FrontEnd;

namespace Humphrey.Backend
{
    public class SymbolTable
    {
        private readonly Dictionary<string, (CompilationType, IType)> typeTable;
        private readonly Dictionary<string, CompilationFunction> functionTable;
        private readonly Dictionary<string, CompilationValue> valueTable;

        public SymbolTable()
        {
            typeTable = new Dictionary<string, (CompilationType, IType)>();
            functionTable = new Dictionary<string, CompilationFunction>();
            valueTable = new Dictionary<string, CompilationValue>();
        }

        public (CompilationType compilationType, IType originalType) FetchType(string identifier)
        {
            if (typeTable.TryGetValue(identifier,out var result))
                return result;
            return (null, null);
        }

        public bool TypeDefined(string identifier)
        {
            return typeTable.ContainsKey(identifier);
        }

        public void AddType(string identifier, CompilationType type, IType originalType)
        {
            typeTable.Add(identifier, (type, originalType));
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