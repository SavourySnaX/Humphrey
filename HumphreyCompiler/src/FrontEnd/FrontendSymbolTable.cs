using System.Collections.Generic;

namespace Humphrey.FrontEnd
{
    public class FrontendSymbolTable
    {
        private readonly Dictionary<string, (IType, SemanticPass.SemanticInfo)> typeTable;
        private readonly Dictionary<string, (IType, SemanticPass.SemanticInfo)> functionTable;
        private readonly Dictionary<string, (IType, SemanticPass.SemanticInfo)> valueTable;

        public FrontendSymbolTable()
        {
            typeTable = new Dictionary<string, (IType, SemanticPass.SemanticInfo)>();
            functionTable = new Dictionary<string, (IType, SemanticPass.SemanticInfo)>();
            valueTable = new Dictionary<string, (IType, SemanticPass.SemanticInfo)>();
        }

        public (IType type, SemanticPass.SemanticInfo info) FetchType(string identifier)
        {
            if (typeTable.TryGetValue(identifier,out var result))
                return result;
            return (null,null);
        }

        public bool TypeDefined(string identifier)
        {
            return typeTable.ContainsKey(identifier) || functionTable.ContainsKey(identifier) || valueTable.ContainsKey(identifier);
        }

        public void AddType(string identifier, IType originalType, SemanticPass.SemanticInfo info)
        {
            typeTable.Add(identifier, (originalType, info));
        }

        public (IType type, SemanticPass.SemanticInfo info) FetchFunction(string identifier)
        {
            if (functionTable.TryGetValue(identifier,out var result))
                return result;
            return (null,null);
        }

        public bool AddFunction(string identifier, IType function, SemanticPass.SemanticInfo info)
        {
            if (functionTable.ContainsKey(identifier))
                return false;
            functionTable.Add(identifier, (function, info));
            return true;
        }

        public (IType type, SemanticPass.SemanticInfo info) FetchValue(string identifier)
        {
            if (valueTable.TryGetValue(identifier,out var result))
                return result;
            return (null,null);
        }

        public bool AddValue(string identifier, IType value, SemanticPass.SemanticInfo info)
        {
            if (valueTable.ContainsKey(identifier))
                return false;
            valueTable.Add(identifier, (value, info));
            return true;
        }

    }
}