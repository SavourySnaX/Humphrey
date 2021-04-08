
using System.Collections.Generic;

namespace Humphrey.FrontEnd
{
    public class SemanticPass
    {
        SemanticScope symbolScopes;
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
            symbolScopes = new SemanticScope();
            symbolScopes.PushScope(moduleName);
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
            var type = symbolScopes.FetchValue(identifier.Name);
            if (type != null)
                return type;
            type = symbolScopes.FetchType(identifier.Name);
            if (type != null)
                return type;
            type = symbolScopes.FetchFunction(identifier.Name);
            return type;
        }

        public IType ResolveValueType(IIdentifier identifier)
        {
            var res = symbolScopes.FetchValue(identifier.Name);
            if (res==null)
            {
                Missing(identifier.Name);
                res = symbolScopes.FetchValue(identifier.Name);
            }
            return res;
        }

        public IType FetchNamedType(IIdentifier identifier)
        {
            var res = FetchAnyType(identifier);
            if (res == null)
            {
                Missing(identifier.Name);
                res = FetchAnyType(identifier);
            }
            if (res== null)
            {
                Messages.Log(CompilerErrorKind.Error_UndefinedType, $"Type '{identifier.Name}' is not found in the current scope.", identifier.Token.Location, identifier.Token.Remainder);
            }
            return res;
        }

        private void Missing(string identifier)
        {
            if (pendingDefinitions.TryGetValue(identifier, out var definition))
            {
                symbolScopes.SaveScopes();
                foreach (var ident in definition.Identifiers)
                {
                    pendingDefinitions.Remove(ident.Dump());
                }
                definition.Semantic(this);
                symbolScopes.RestoreScopes();
            }
        }

        public bool AddFunction(IIdentifier identifier, IType type)
        {
            var baseT = type.ResolveBaseType(this);
            var s = new SemanticPass.SemanticInfo(type, baseT, SemanticPass.IdentifierKind.Function);
            var ok = symbolScopes.AddFunction(identifier.Name, type, s);
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
            var ok = symbolScopes.AddType(identifier.Name, type, s);
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
            var ok = symbolScopes.AddValue(identifier.Name, type, s);
            if (ok && addSemanticInfo)  // addSemanticInfo is bodge to ensure function pointer delegate types work
            {
                semanticInfo.Add(identifier.Token, s);
            }
            return ok;
        }

        public bool AddSemanticLocation(IIdentifier identifier, Result<Tokens> token)
        {
            var t = symbolScopes.FetchAny(identifier.Name);
            if (t.type == null)
            {
                Missing(identifier.Name);
                t = symbolScopes.FetchAny(identifier.Name);
                if (t.type==null)
                    return false;
            }
            semanticInfo.Add(token, t.info);
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

        public void PushScope(string scope)
        {
            symbolScopes.PushScope(scope);
        }

        public void PopScope()
        {
            symbolScopes.PopScope();
        }

        public ICompilerMessages Messages => messages;
    }
}