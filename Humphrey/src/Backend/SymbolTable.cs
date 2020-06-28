using System.Collections.Generic;

namespace Humphrey.Backend
{
    public class SymbolTable
    {
        private readonly Dictionary<string, CompilationFunction> functionTable;
        private readonly Dictionary<(string, CompilationFunction), CompilationValue> inputParamsTable;

        public SymbolTable()
        {
            functionTable = new Dictionary<string, CompilationFunction>();
            inputParamsTable = new Dictionary<(string, CompilationFunction), CompilationValue>();
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
    }
}