using System.Collections.Generic;

namespace Humphrey.FrontEnd
{
    public class SemanticScope
    {
        private List<(string name, FrontendSymbolTable symbols)> scopeStack;

        private Stack<List<(string name, FrontendSymbolTable symbols)>> scopeSave;

        public SemanticScope()
        {
            scopeStack = new List<(string,FrontendSymbolTable)>();
            scopeSave = new Stack<List<(string name, FrontendSymbolTable symbols)>>();
        }

        public void PushScope(string scope)
        {
            scopeStack.Add((scope, new FrontendSymbolTable()));
        }

        public void PopScope()
        {
            scopeStack.RemoveAt(scopeStack.Count - 1);
        }

        public void SaveScopes()
        {
            var firstItem = scopeStack[0];
            scopeSave.Push(scopeStack);
            scopeStack = new List<(string name, FrontendSymbolTable symbols)>();
            scopeStack.Add(firstItem);
        }

        public void RestoreScopes()
        {
            scopeStack = scopeSave.Pop();
        }

        private delegate bool AlreadyPresentDelegate(FrontendSymbolTable symbolTable);
        private delegate void AddItemDelegate(FrontendSymbolTable symbolTable);

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

        public bool AddType(string identifier, IType originalType, SemanticPass.SemanticInfo info)
        {
            return AddItem(identifier, (s) => s.TypeDefined(identifier), (s) => s.AddType(identifier, originalType, info));
        }

        public bool AddFunction(string identifier, IType function, SemanticPass.SemanticInfo info)
        {
            return AddItem(identifier, (s) => s.TypeDefined(identifier), (s) => s.AddFunction(identifier, function, info));
        }

        public bool AddValue(string identifier, IType value, SemanticPass.SemanticInfo info)
        {
            return AddItem(identifier, (s) => s.TypeDefined(identifier), (s) => s.AddValue(identifier, value, info));
        }

        public (IType type, SemanticPass.SemanticInfo info) FetchAny(string identifier)
        {
            IType found = null;
            int stackIdx = scopeStack.Count - 1;
            while (found==null && stackIdx>=0)
            {
                var value = scopeStack[stackIdx].symbols.FetchValue(identifier);
                if (value.type != null)
                    return value;
                value = scopeStack[stackIdx].symbols.FetchType(identifier);
                if (value.type != null)
                    return value;
                value = scopeStack[stackIdx].symbols.FetchFunction(identifier);
                if (value.type != null)
                    return value;
                stackIdx--;
            }

            return (null, null);
        }

        public IType FetchType(string identifier)
        {
            IType found = null;
            int stackIdx = scopeStack.Count - 1;
            while (found==null && stackIdx>=0)
            {
                var value = scopeStack[stackIdx].symbols.FetchType(identifier);
                if (value.type != null)
                    return value.type;
                stackIdx--;
            }

            return null;
        }

        public IType FetchValue(string identifier)
        {
            int stackIdx = scopeStack.Count - 1;
            while (stackIdx>=0)
            {
                var value = scopeStack[stackIdx].symbols.FetchValue(identifier);
                if (value.type != null)
                    return value.type;
                stackIdx--;
            }

            return null;
        }

        public IType FetchFunction(string identifier)
        {
            int stackIdx = scopeStack.Count - 1;
            while (stackIdx>=0)
            {
                var value = scopeStack[stackIdx].symbols.FetchFunction(identifier);
                if (value.type != null)
                    return value.type;
                stackIdx--;
            }

            return null;
        }
    }
}