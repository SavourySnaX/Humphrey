using System.Collections.Generic;

namespace Humphrey.Backend
{
    public class SymbolTable
    {
        private readonly Dictionary<string, CompilationType> typeTable;
        private readonly Dictionary<string, CompilationFunction> functionTable;
        private readonly Dictionary<string, CompilationValue> globalValueTable;
        private readonly Dictionary<(string, CompilationFunction), CompilationValue> inputParamsTable;

        public SymbolTable()
        {
            typeTable = new Dictionary<string, CompilationType>();
            functionTable = new Dictionary<string, CompilationFunction>();
            globalValueTable = new Dictionary<string, CompilationValue>();
            inputParamsTable = new Dictionary<(string, CompilationFunction), CompilationValue>();
        }

        public CompilationType FetchType(string identifier)
        {
            if (typeTable.TryGetValue(identifier,out var result))
                return result;
            return null;
        }

        public bool AddType(string identifier, CompilationType type)
        {
            if (typeTable.ContainsKey(identifier))
                return false;
            typeTable.Add(identifier, type);
            return true;
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

        public CompilationValue FetchFunctionParam(string identifier, CompilationFunction function)
        {
            if (inputParamsTable.TryGetValue((identifier,function),out var result))
                return result;
            return null;
        }

        public bool AddFunctionParam(string identifier, CompilationFunction function, CompilationValue value)
        {
            if (inputParamsTable.ContainsKey((identifier,function)))
                return false;
            inputParamsTable.Add((identifier, function), value);
            return true;
        }

        public CompilationValue FetchGlobalValue(string identifier)
        {
            if (globalValueTable.TryGetValue(identifier,out var result))
                return result;
            return null;
        }

        public bool AddGlobalValue(string identifier, CompilationValue value)
        {
            if (globalValueTable.ContainsKey(identifier))
                return false;
            globalValueTable.Add(identifier, value);
            return true;
        }
    }
}