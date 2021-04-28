using System.Collections.Generic;
using Humphrey.FrontEnd;

namespace Humphrey
{
    public class CommonSymbolTable
    {
        private readonly Dictionary<string, CommonSymbolTableEntry> _typeTable;
        private readonly Dictionary<string, CommonSymbolTableEntry> _functionTable;
        private readonly Dictionary<string, CommonSymbolTableEntry> _valueTable;
        private readonly Dictionary<string, (CommonSymbolTable symbols, IPackageLevel packageLevel)> _namespaceTable;

        public Dictionary<string, IGlobalDefinition> pendingDefinitions;

        private CommonSymbolTable _parent;

        public CommonSymbolTable(CommonSymbolTable parent)
        {
            _typeTable = new Dictionary<string, CommonSymbolTableEntry>();
            _functionTable = new Dictionary<string, CommonSymbolTableEntry>();
            _valueTable = new Dictionary<string, CommonSymbolTableEntry>();
            _namespaceTable = new Dictionary<string, (CommonSymbolTable symbols, IPackageLevel packageLevel)>();
            _parent = parent;
            pendingDefinitions = null;
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

        public bool AddNamespace(string identifier, IPackageLevel level)
        {
            return AddNamespace(identifier, level, new CommonSymbolTable(null), null);
        }

        public bool AddNamespace(string identifier, IPackageLevel level, CommonSymbolTable entry, IGlobalDefinition[] pending)
        {
            if (FetchNamespace(identifier)!=null)
                return false;

            var global = this;
            while (global._parent!=null)
            {
                global = global._parent;
            }
            // We always add the namespace to the root of the symbol table... I think
            if (pending != null)
            {
                entry.pendingDefinitions = new Dictionary<string, IGlobalDefinition>();
                foreach (var def in pending)
                {
                    foreach (var ident in def.Identifiers)
                    {
                        entry.pendingDefinitions.Add(ident.Dump(), def);
                    }
                }
            }
            global._namespaceTable.Add(identifier, (entry, level));
            return true;
        }

        public CommonSymbolTable FetchNamespace(string identifier)
        {
            if (_namespaceTable.TryGetValue(identifier, out var result))
                return result.symbols;
            if (_parent!=null)
                return Parent.FetchNamespace(identifier);
            return null;
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

        internal void MergeSymbolTable(CommonSymbolTable rootSymbolTable)
        {
            // Do types
            foreach (var e in rootSymbolTable._typeTable)
            {
                AddType(e.Key, e.Value);
            }
            foreach (var e in rootSymbolTable._functionTable)
            {
                AddFunction(e.Key, e.Value);
            }
            foreach (var e in rootSymbolTable._valueTable)
            {
                AddValue(e.Key, e.Value);
            }
        }
    }
}
