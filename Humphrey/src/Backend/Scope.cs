
using System.Collections.Generic;
using Humphrey.FrontEnd;
using LLVMSharp.Interop;

namespace Humphrey.Backend
{
    public class Scope
    {
        private List<(string name, SymbolTable symbols)> scopeStack;

        private Stack<List<(string name, SymbolTable symbols)>> scopeSave;
        private List<LLVMMetadataRef> debugScopeStack;

        private Stack<List<LLVMMetadataRef>> debugScopeSave;

        public Scope()
        {
            scopeStack = new List<(string,SymbolTable)>();
            scopeSave = new Stack<List<(string name, SymbolTable symbols)>>();
            debugScopeSave = new Stack<List<LLVMMetadataRef>>();
            debugScopeStack = new List<LLVMMetadataRef>();
        }

        public void PushScope(string scope, LLVMMetadataRef debugScope)
        {
            scopeStack.Add((scope, new SymbolTable()));
            debugScopeStack.Add(debugScope);
        }

        public void PopScope()
        {
            scopeStack.RemoveAt(scopeStack.Count - 1);
            debugScopeStack.RemoveAt(debugScopeStack.Count - 1);
        }

        public LLVMMetadataRef CurrentDebugScope => debugScopeStack[debugScopeStack.Count - 1];

        public void SaveScopes()
        {
            var firstItem = scopeStack[0];
            scopeSave.Push(scopeStack);
            scopeStack = new List<(string name, SymbolTable symbols)>();
            scopeStack.Add(firstItem);
            var firstDebugItem = debugScopeStack[0];
            debugScopeSave.Push(debugScopeStack);
            debugScopeStack = new List<LLVMMetadataRef>();
            debugScopeStack.Add(firstDebugItem);
        }

        public void RestoreScopes()
        {
            scopeStack = scopeSave.Pop();
            debugScopeStack = debugScopeSave.Pop();
        }

        private delegate bool AlreadyPresentDelegate(SymbolTable symbolTable);
        private delegate void AddItemDelegate(SymbolTable symbolTable);

        private bool AddItem(string identifier, AlreadyPresentDelegate alreadyPresent, AddItemDelegate addItem)
        {
            int stackIdx = scopeStack.Count - 1;
            while (stackIdx>=0)
            {
                if (alreadyPresent(scopeStack[stackIdx].symbols))
                    return false;
                stackIdx--;
            }

            addItem(scopeStack[scopeStack.Count - 1].symbols);
            return true;

        }

        public bool AddType(string identifier, CompilationType type, IType originalType)
        {
            return AddItem(identifier, (s) => s.TypeDefined(identifier), (s) => s.AddType(identifier, type, originalType));
        }

        public bool AddFunction(string identifier, CompilationFunction function)
        {
            return AddItem(identifier, (s) => s.TypeDefined(identifier), (s) => s.AddFunction(identifier, function));
        }

        public bool AddValue(string identifier, CompilationValue value)
        {
            return AddItem(identifier, (s) => s.TypeDefined(identifier), (s) => s.AddValue(identifier, value));
        }


        // Fetch type based on the current scope
        public (CompilationType compilationType, IType originalType) FetchNamedType(string identifier)
        {
            CompilationValue found = null;
            int stackIdx = scopeStack.Count - 1;
            while (found==null && stackIdx>=0)
            {
                var type=scopeStack[stackIdx].symbols.FetchType(identifier);
                if (type.compilationType != null)
                    return type;
                stackIdx--;
            }

            return (null, null);
        }

        private CompilationValue FetchValueInternal(CompilationUnit unit, SymbolTable symbolTable, string identifier, CompilationBuilder builder)
        {
            // Check for value
            var value = symbolTable.FetchValue(identifier);
            if (value != null)
            {
                return builder.Load(value);
            }

            // Check for function - i guess we can construct this on the fly?
            var function = symbolTable.FetchFunction(identifier);
            if (function != null)
            {
                value = new CompilationValue(function.BackendValue, function.FunctionType);
                return value;
            }

            // Check for named type (an enum is actually a value type) - might be better done at definition actually
            var nType = symbolTable.FetchType(identifier).compilationType;
            if (nType != null)
            {
                value = unit.CreateUndef(nType);
                return value;
            }

            return null;

        }

        // Fetch value based on the current scope
        public CompilationValue FetchValue(CompilationUnit unit, string identifier, CompilationBuilder builder)
        {
            CompilationValue found = null;
            int stackIdx = scopeStack.Count - 1;
            while (found==null && stackIdx>=0)
            {
                var value = FetchValueInternal(unit, scopeStack[stackIdx].symbols, identifier, builder);
                if (value != null)
                    return value;
                stackIdx--;
            }

            return null;
        }

        private CompilationValue FetchLocationInternal(SymbolTable symbolTable, string identifier, CompilationBuilder builder)
        {
            // Check for value
            var value = symbolTable.FetchValue(identifier);
            if (value != null)
            {
                return value.Storage;
            }
            return null;
        }

        public CompilationValue FetchLocation(string identifier, CompilationBuilder builder)
        {
            int stackIdx = scopeStack.Count - 1;
            while (stackIdx>=0)
            {
                var value = FetchLocationInternal(scopeStack[stackIdx].symbols, identifier, builder);
                if (value != null)
                    return value;
                stackIdx--;
            }

            return null;
        }
        public CompilationValue FetchValue(string identifier)
        {
            int stackIdx = scopeStack.Count - 1;
            while (stackIdx>=0)
            {
                var value = scopeStack[stackIdx].symbols.FetchValue(identifier);
                if (value != null)
                    return value;
                stackIdx--;
            }

            return null;
        }

        public CompilationFunction FetchFunction(string identifier)
        {
            int stackIdx = scopeStack.Count - 1;
            while (stackIdx>=0)
            {
                var value = scopeStack[stackIdx].symbols.FetchFunction(identifier);
                if (value != null)
                    return value;
                stackIdx--;
            }

            return null;
        }
    }
}