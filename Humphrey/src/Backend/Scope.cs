
using System.Collections.Generic;

namespace Humphrey.Backend
{
    public class Scope
    {
        private List<(string name, SymbolTable symbols)> scopeStack;

        private Stack<List<(string name, SymbolTable symbols)>> scopeSave;

        public Scope()
        {
            scopeStack = new List<(string,SymbolTable)>();
            scopeSave = new Stack<List<(string name, SymbolTable symbols)>>();
        }

        public void PushScope(string scope)
        {
            scopeStack.Add((scope, new SymbolTable()));
        }

        public void PopScope()
        {
            scopeStack.RemoveAt(scopeStack.Count - 1);
        }

        public void SaveScopes()
        {
            var firstItem = scopeStack[0];
            scopeSave.Push(scopeStack);
            scopeStack = new List<(string name, SymbolTable symbols)>();
            scopeStack.Add(firstItem);
        }

        public void RestoreScopes()
        {
            scopeStack = scopeSave.Pop();
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

        public bool AddType(string identifier, CompilationType type)
        {
            return AddItem(identifier, (s) => s.TypeDefined(identifier), (s) => s.AddType(identifier, type));
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
        public CompilationType FetchNamedType(string identifier)
        {
            CompilationValue found = null;
            int stackIdx = scopeStack.Count - 1;
            while (found==null && stackIdx>=0)
            {
                var type=scopeStack[stackIdx].symbols.FetchType(identifier);
                if (type!=null)
                    return type;
                stackIdx--;
            }

            return null;
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
            if (function!=null)
            {
                value = new CompilationValue(function.BackendValue, function.FunctionType);
                return value;
            }

            // Check for named type (an enum is actually a value type) - might be better done at definition actually
            var nType = symbolTable.FetchType(identifier);
            if (nType!=null)
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