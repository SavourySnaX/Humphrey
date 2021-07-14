
using System.Collections.Generic;
using System.Text;

namespace Humphrey.FrontEnd
{
    public class SemanticPass
    {
        public struct SymbolTableAndPass
        {
            public SemanticPass        pass;
            public CommonSymbolTable   symbols;
        }

        Stack<CommonSymbolTable> symbolStack;
        CommonSymbolTable root;
        CommonSymbolTable currentScope;

        ICompilerMessages messages;
        Dictionary<string, IGlobalDefinition> pendingDefinitions;
        Dictionary<string, IGlobalDefinition> usedDefinitions;
        List<SymbolTableAndPass> importedNamespaces;
        Dictionary<string,SymbolTableAndPass> allImportedNamespaces;
        List<IGlobalDefinition> pendingCompile;

        Dictionary<Result<Tokens>, SemanticInfo> semanticInfo;

        IPackageManager currentManager; 
        IPackageLevel packageRoot;          // level could match managerRoot but doesn't have to

        public class SemanticInfo
        {
            private IType _ast;
            private IType _base;
            private IdentifierKind _kind;

            public SemanticInfo(IType type, IType baseT, IdentifierKind kind)
            {
                _ast = type;
                _base = baseT;
                _kind = kind;
                if (kind == IdentifierKind.Type && _base != null)
                    _kind = _base.GetBaseType;
            }

            public IType Ast => _ast;
            public IType Base => _base;
            public IdentifierKind Kind => _kind;
        }

        public enum IdentifierKind
        {
            None,
            Function,
            Type,
            StructType,
            StructMember,
            EnumType,
            EnumMember,
            GlobalValue,
            FunctionParam,
            LocalValue,
            AliasType
        }

        public SemanticPass(IPackageManager manager, ICompilerMessages overrideDefaultMessages = null)
        {
            var root = manager?.FetchRoot;
            ConstructSemanticPass(manager, root, new Dictionary<string, SymbolTableAndPass>(), overrideDefaultMessages);
        }

        protected SemanticPass(IPackageManager manager, IPackageLevel level, ICompilerMessages overrideDefaultMessages = null)
        {
            ConstructSemanticPass(manager, level, new Dictionary<string, SymbolTableAndPass>(), overrideDefaultMessages);
        }
        protected SemanticPass(IPackageManager manager, IPackageLevel level, Dictionary<string,SymbolTableAndPass> allImported, ICompilerMessages overrideDefaultMessages = null)
        {
            ConstructSemanticPass(manager, level, allImported, overrideDefaultMessages);
        }

        protected void ConstructSemanticPass(IPackageManager manager, IPackageLevel level, Dictionary<string,SymbolTableAndPass> allImports, ICompilerMessages overrideDefaultMessages = null)
        {
            messages = overrideDefaultMessages;
            if (messages==null)
                messages = new CompilerMessages(true, true, false);
            symbolStack = new Stack<CommonSymbolTable>();
            root = new CommonSymbolTable(null);
            currentScope = root;
            currentManager = manager;
            packageRoot = level;
            semanticInfo = new Dictionary<Result<Tokens>, SemanticInfo>();
            pendingCompile = new List<IGlobalDefinition>();
            pendingDefinitions = new Dictionary<string, IGlobalDefinition>();
            usedDefinitions = new Dictionary<string, IGlobalDefinition>();
            importedNamespaces = new List<SymbolTableAndPass>();
            allImportedNamespaces = allImports;
        }

        public void AddToPending(IGlobalDefinition[] globals)
        {
            pendingCompile.AddRange(globals);
            root.pendingDefinitions=new Dictionary<string, IGlobalDefinition>();

            foreach (var def in globals)
            {
                if (!(def is AstUsingNamespace))
                {
                    foreach (var ident in def.Identifiers)
                    {
                        if (pendingDefinitions.ContainsKey(ident.Name))
                        {
                            Messages.Log(CompilerErrorKind.Error_DuplicateSymbol, $"A symbol called {ident.Name} already exists", ident.Token.Location, ident.Token.Remainder);
                            continue;
                        }
                        pendingDefinitions.Add(ident.Name, def);
                        root.pendingDefinitions.Add(ident.Name, def);
                    }
                }
            }

            // Process using statements second (this way pending definitions are correctly setup)
            foreach (var def in globals)
            {
                if (def is AstUsingNamespace usingNamespace)
                {
                    usingNamespace.Semantic(this);
                }
            }
        }

        public void RunPass(IGlobalDefinition[] globals)
        {
            AddToPending(globals);
            while (pendingDefinitions.Count!=0)
            {
                var enumerator = pendingDefinitions.Keys.GetEnumerator();
                enumerator.MoveNext();
                Missing(enumerator.Current);
            }
        }

        private IType FetchAnyType(IIdentifier identifier)
        {
            var entry = currentScope.FetchAny(identifier.Name);
            return entry?.AstType;
        }

        public IType ResolveValueType(IIdentifier identifier)
        {
            var entry = currentScope.FetchValue(identifier.Name);
            if (entry==null)
            {
                // Need to look in our imported namespaces too
                foreach (var s in importedNamespaces)
                {
                    if (s.pass.pendingDefinitions.ContainsKey(identifier.Name))
                    {
                        s.pass.Missing(identifier.Name);
                    }
                    entry = s.symbols.FetchValue(identifier.Name);
                    if (entry !=null)
                    {
                        return entry?.AstType;
                    }
                }
                Missing(identifier.Name);
                entry = currentScope.FetchValue(identifier.Name);
            }
            return entry?.AstType;
        }

        public IType FetchNamedType(IIdentifier identifier)
        {
            var type = FetchAnyType(identifier);
            if (type == null)
            {
                // Need to look in our imported namespaces too
                foreach (var s in importedNamespaces)
                {
                    if (s.pass.pendingDefinitions.ContainsKey(identifier.Name))
                    {
                        s.pass.Missing(identifier.Name);
                    }
                    type = s.symbols.FetchAny(identifier.Name)?.AstType;
                    if (type !=null)
                    {
                        return type;
                    }
                }
                Missing(identifier.Name);
                type = FetchAnyType(identifier);
            }
            if (type== null)
            {
                Messages.Log(CompilerErrorKind.Error_UndefinedType, $"Type '{identifier.Name}' is not found in the current scope.", identifier.Token.Location, identifier.Token.Remainder);
            }
            return type;
        }

        private void Missing(string identifier)
        {
            if (pendingDefinitions.TryGetValue(identifier, out var definition))
            {
                // Need to return to the root symbol scope here
                symbolStack.Push(currentScope);
                currentScope=root;
                foreach (var ident in definition.Identifiers)
                {
                    usedDefinitions.Add(ident.Name,definition);
                    pendingDefinitions.Remove(ident.Name);
                }
                definition.Semantic(this);
                currentScope= symbolStack.Peek();
                symbolStack.Pop();
            }
        }

        public bool AddFunction(IIdentifier identifier, IType type)
        {
            var baseT = type.ResolveBaseType(this);
            var s = new SemanticPass.SemanticInfo(type, baseT, SemanticPass.IdentifierKind.Function);
            var ok = currentScope.AddFunction(identifier.Name, new CommonSymbolTableEntry(type, s));
            if (ok)
            {
                semanticInfo.Add(identifier.Token, s);
            }
            return ok;
        }

        public bool AddType(IIdentifier identifier, IType type)
        {
            var baseT = type.ResolveBaseType(this);
            var s = new SemanticPass.SemanticInfo(type, baseT, SemanticPass.IdentifierKind.Type);
            var ok = currentScope.AddType(identifier.Name, new CommonSymbolTableEntry(type, s));
            if (ok)
            {
                semanticInfo.Add(identifier.Token, s);
            }
            return ok;
        }

        public bool AddValue(IIdentifier identifier, SemanticPass.IdentifierKind kind, IType type, bool addSemanticInfo = true)
        {
            var baseT = type.ResolveBaseType(this);
            var s = new SemanticPass.SemanticInfo(type, baseT, kind);
            var ok = currentScope.AddValue(identifier.Name, new CommonSymbolTableEntry(type, s));
            if (ok && addSemanticInfo)  // addSemanticInfo is bodge to ensure function pointer delegate types work
            {
                semanticInfo.Add(identifier.Token, s);
            }
            return ok;
        }

        public void AddSemanticInfoToToken(SemanticInfo info, Result<Tokens> token)
        {
            semanticInfo.Add(token, info);
        }

        public bool AddSemanticLocation(IIdentifier identifier, Result<Tokens> token)
        {
            var entry = currentScope.FetchAny(identifier.Name);
            if (entry == null)
            {
                // Need to look in our imported namespaces too
                foreach (var s in importedNamespaces)
                {
                    if (s.pass.pendingDefinitions.ContainsKey(identifier.Name))
                    {
                        s.pass.Missing(identifier.Name);
                    }
                    entry = s.symbols.FetchAny(identifier.Name);
                    if (entry !=null)
                    {
                        semanticInfo.Add(token, entry.SemanticInfo);
                        return true;
                    }
                }
                Missing(identifier.Name);
                entry = currentScope.FetchAny(identifier.Name);
                if (entry == null)
                    return false;
            }
            semanticInfo.Add(token, entry.SemanticInfo);
            return true;
        }

        public void AddStructElementLocation(Result<Tokens> token, IType type)
        {
            var s = new SemanticPass.SemanticInfo(type, type.ResolveBaseType(this), IdentifierKind.StructMember);
            semanticInfo.Add(token, s);
        }
        
        public void AddEnumElementLocation(Result<Tokens> token, IType type)
        {
            var s = new SemanticPass.SemanticInfo(type, type.ResolveBaseType(this), IdentifierKind.EnumMember);
            if (token.HasValue)
            {
                semanticInfo.Add(token, s);
            }
        }


        public bool FetchSemanticInfo(Result<Tokens> token, out SemanticInfo info)
        {
            return semanticInfo.TryGetValue(token, out info);
        }

        public CommonSymbolTable PushScope()
        {
            currentScope = new CommonSymbolTable(currentScope);
            return currentScope;
        }

        public void ImportNamespace(IIdentifier[] scope)
        {
            var scopeName = new StringBuilder();
            // For now, pulls ALL in 
            // so locate the correct package point to start pulling in
            var cLevel = currentManager.FetchRoot;  // Always import from global root
            foreach (var s in scope)
            {
                if (scopeName.Length>0)
                {
                    scopeName.Append(".");
                }
                scopeName.Append(s.Name);

                cLevel = cLevel.FetchEntry(s.Name);
                if (cLevel == null)
                {
                    messages.Log(CompilerErrorKind.Error_UnknownNamespace, $"Unknown Namespace {scopeName.ToString()}", s.Token.Location, s.Token.Remainder);
                    return;
                }
            }
            if (cLevel == null)
            {
                throw new System.Exception($"TODO error unknown namespace");
            }

            // At this point we either have a level (and thus a list of named entries)
            //or we have an Entry, in which case we import the entry
            if (cLevel is IPackageEntry packageEntry)
            {
                var scopeNameString = scopeName.ToString();
                var newScope = PushScope();
                if (!allImportedNamespaces.ContainsKey(scopeNameString))
                {
                    var t = new HumphreyTokeniser(messages);
                    var p = new HumphreyParser(t.Tokenize(packageEntry.Contents, packageEntry.Path), messages);
                    var globals = p.File();
                    var sp = new SemanticPass(currentManager, cLevel, allImportedNamespaces, messages);

                    var s = new SymbolTableAndPass{ pass = sp, symbols = sp.root};
                    allImportedNamespaces.Add(scopeName.ToString(), s);
                    importedNamespaces.Add(s);
                    sp.AddToPending(globals);
                }
                else
                {
                    importedNamespaces.Add(allImportedNamespaces[scopeNameString]);
                }
            }
            else
            {
                throw new System.NotImplementedException($"TODO");
            }
        }

        public (CommonSymbolTable recoverTo, CommonSymbolTable root) PushNamespace(IIdentifier[] scope)
        {
            var oldScope = currentScope;
            var cScope = currentScope;
            var cLevel = packageRoot;

            foreach (var s in scope)
            {
                var result = cScope.FetchNamespace(s.Name);
                if (result == null)
                {
                    var entry = cLevel.FetchEntry(s.Name);
                    if (entry==null)
                    {
                        throw new System.Exception($"TODO - error unknown namespace");
                    }
                    if (entry is IPackageEntry packageEntry)
                    {
                        // this would be a file, and thus should be compiled i think...
                        var t = new HumphreyTokeniser(messages);
                        var p = new HumphreyParser(t.Tokenize(packageEntry.Contents, packageEntry.Path), messages);
                        var globals = p.File();
                        var sp = new SemanticPass(currentManager,entry, messages);
                        sp.RunPass(globals);
                        cScope.AddNamespace(s.Name, entry, sp.RootSymbolTable, globals);
                        result = cScope.FetchNamespace(s.Name);
                    }
                    else
                    {
                        cScope.AddNamespace(s.Name, cLevel);
                        result = cScope.FetchNamespace(s.Name);
                    }
                    cLevel = entry;
                }
                cScope = result;
            }
            currentScope = cScope;
            return (oldScope, currentScope);
        }

        public void PopNamespace(CommonSymbolTable recoverTo)
        {
            currentScope = recoverTo;
        }

        public void PopScope()
        {
            currentScope = currentScope.Parent;
        }

        public IPackageManager Manager => currentManager;
        public IEnumerable<IGlobalDefinition> ToCompile => pendingCompile;
        public IEnumerable<SymbolTableAndPass> ImportedNamespaces => allImportedNamespaces.Values;
        public ICompilerMessages Messages => messages;
        public CommonSymbolTable RootSymbolTable => root;

        public Dictionary<string, IGlobalDefinition> UsedDefinitions => usedDefinitions;
    }
}