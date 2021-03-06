
using System.Collections.Generic;

namespace Humphrey.FrontEnd
{
    public class SemanticPass
    {
        Stack<CommonSymbolTable> symbolStack;
        CommonSymbolTable root;
        CommonSymbolTable currentScope;

        ICompilerMessages messages;
        Dictionary<string, IGlobalDefinition> pendingDefinitions;

        Dictionary<Result<Tokens>, SemanticInfo> semanticInfo;

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
            LocalValue
        }

        public SemanticPass(string sourceFileNameAndPath, ICompilerMessages overrideDefaultMessages = null)
        {
            messages = overrideDefaultMessages;
            if (messages==null)
                messages = new CompilerMessages(true, true, false);
            var moduleName = System.IO.Path.GetFileNameWithoutExtension(sourceFileNameAndPath);
            symbolStack = new Stack<CommonSymbolTable>();
            root = new CommonSymbolTable(null);
            currentScope = root;
            semanticInfo = new Dictionary<Result<Tokens>, SemanticInfo>();
        }

        public void RunPass(IGlobalDefinition[] globals)
        {
            pendingDefinitions = new Dictionary<string, IGlobalDefinition>();
            foreach (var def in globals)
            {
                foreach (var ident in def.Identifiers)
                {
                    if (pendingDefinitions.ContainsKey(ident.Name))
                    {
                        Messages.Log(CompilerErrorKind.Error_DuplicateSymbol, $"A symbol called {ident.Name} already exists", ident.Token.Location, ident.Token.Remainder);
                        continue;
                    }
                    pendingDefinitions.Add(ident.Name, def);
                }
            }

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
                    pendingDefinitions.Remove(ident.Dump());
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

        public bool AddSemanticLocation(IIdentifier identifier, Result<Tokens> token)
        {
            var entry = currentScope.FetchAny(identifier.Name);
            if (entry == null)
            {
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
            semanticInfo.Add(token, s);
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

        public void PopScope()
        {
            currentScope = currentScope.Parent;
        }

        public ICompilerMessages Messages => messages;
        public CommonSymbolTable RootSymbolTable => root;
    }
}