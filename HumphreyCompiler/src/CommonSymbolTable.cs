using System.Collections.Generic;

namespace Humphrey
{
    public class CommonSymbolTable
    {
        private readonly Dictionary<string, CommonSymbolTableEntry> _typeTable;
        private readonly Dictionary<string, CommonSymbolTableEntry> _functionTable;
        private readonly Dictionary<string, CommonSymbolTableEntry> _valueTable;

        private CommonSymbolTable _parent;

        public CommonSymbolTable(CommonSymbolTable parent)
        {
            _typeTable = new Dictionary<string, CommonSymbolTableEntry>();
            _functionTable = new Dictionary<string, CommonSymbolTableEntry>();
            _valueTable = new Dictionary<string, CommonSymbolTableEntry>();
            _parent = parent;
        }

        public CommonSymbolTableEntry FetchType(string identifier)
        {
            if (_typeTable.TryGetValue(identifier,out var result))
                return result;
            if (_parent!=null)
                return _parent.FetchType(identifier);
            return null;
        }

        public bool TypeDefined(string identifier)
        {
            return _typeTable.ContainsKey(identifier) || _functionTable.ContainsKey(identifier) || _valueTable.ContainsKey(identifier) || _parent.TypeDefined(identifier);
        }

        public bool AddType(string identifier, CommonSymbolTableEntry entry)
        {
            if (FetchType(identifier) != null)
                return false;
            _typeTable.Add(identifier, entry);
            return true;
        }

        public CommonSymbolTableEntry FetchFunction(string identifier)
        {
            if (_functionTable.TryGetValue(identifier,out var result))
                return result;
            if (_parent!=null)
                return _parent.FetchFunction(identifier);
            return null;
        }

        public bool AddFunction(string identifier, CommonSymbolTableEntry entry)
        {
            if (FetchFunction(identifier) != null)
                return false;
            _functionTable.Add(identifier, entry);
            return true;
        }

        public CommonSymbolTableEntry FetchValue(string identifier)
        {
            if (_valueTable.TryGetValue(identifier,out var result))
                return result;
            if (_parent!=null)
                return _parent.FetchValue(identifier);
            return null;
        }

        public bool AddValue(string identifier, CommonSymbolTableEntry entry)
        {
            if (FetchValue(identifier) != null)
                return false;
            _valueTable.Add(identifier, entry);
            return true;
        }
        
        public CommonSymbolTableEntry FetchAny(string identifier)
        {
            var entry = FetchValue(identifier);
            if (entry != null)
                return entry;
            entry = FetchType(identifier);
            if (entry != null)
                return entry;
            return FetchFunction(identifier);
        }

        public CommonSymbolTable Parent => _parent;

    }
}
