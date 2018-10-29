using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace SmallerLang.Emitting
{
    struct NamespaceContainer
    {
        readonly MethodCache _methods;
        readonly SmallTypeCache _types;
        public string Namespace { get; private set; }
        public string Alias { get; private set; }

        public NamespaceContainer(string pNamespace, string pAlias)
        {
            Namespace = pNamespace;
            Alias = pAlias;
            _methods = MethodCache.Create(pAlias);
            _types = SmallTypeCache.Create(pAlias);
        }

        #region Types
        public SmallType FindType(string pType)
        {
            return _types.FindType(pType);
        }

        public LLVMSharp.LLVMTypeRef GetLLVMTypeOfType(string pType)
        {
            return _types.GetLLVMTypeOfType(pType);
        }

        public void SetLLVMType(string pType, LLVMSharp.LLVMTypeRef pLLVMType)
        {
            _types.SetLLVMType(pType, pLLVMType);
        }

        public SmallType AddType(Syntax.TypeDefinitionSyntax pType)
        {
            return _types.AddType(pType);
        }

        public SmallType AddType(Syntax.EnumSyntax pType)
        {
            return _types.AddType(pType);
        }

        public bool IsTypeDefinedInNamespace(string pType)
        {
            return _types.IsTypeDefinedInNamespace(pType);
        }

        public SmallType GetConcreteType(SmallType pType, params SmallType[] pGenericParameters)
        {
            return _types.GetConcreteType(pType, pGenericParameters);
        }
        #endregion

        #region Methods
        public bool FindCast(out MethodDefinition pDefinition, SmallType pFrom, SmallType pTo)
        {
            return _methods.FindCast(pFrom, pTo, out pDefinition);
        }

        public bool FindMethod(out MethodDefinition pDefinition, out bool pExact, SmallType pType, string pName, params SmallType[] pArguments)
        {
            return _methods.FindMethod(out pDefinition, out pExact, pType, pName, pArguments);
        }

        public bool MethodExists(SmallType pType, Syntax.MethodSyntax pMethod)
        {
            return _methods.MethodExists(pType, pMethod.Name, pMethod);
        }
        
        public IList<MethodDefinition> GetAllMatches(string pName, int pParameterCount)
        {
            return _methods.GetAllMatches(pName, pParameterCount);
        }

        public Syntax.MethodSyntax MatchMethod(Syntax.MethodCallSyntax pCall, IEnumerable<Syntax.MethodSyntax> pCandidates)
        {
            return _methods.MatchMethod(pCall, pCandidates);
        }

        public MethodDefinition AddMethod(SmallType pType, Syntax.MethodSyntax pMethod)
        {
            return _methods.AddMethod(pType, pMethod.Name, pMethod);
        }

        public MethodDefinition AddMethod(Syntax.MethodSyntax pMethod)
        {
            return _methods.AddMethod(pMethod.Name, pMethod);
        }
        #endregion
    }

    static class NamespaceManager
    {
        private static Dictionary<string, NamespaceContainer> _namespaces = new Dictionary<string, NamespaceContainer>();
        static string _stdlibAlias;

        public static NamespaceContainer AddNamespace(string pNamespace, string pAlias)
        {
            if(!_namespaces.ContainsKey(pAlias))
            {
                if (System.IO.Path.GetFileNameWithoutExtension(pNamespace) == "stdlib") _stdlibAlias = pAlias;
                _namespaces.Add(pAlias, new NamespaceContainer(pNamespace, pAlias));
            }
            return _namespaces[pAlias];
        }

        internal static IEnumerable<NamespaceContainer> GetAllNamespaces()
        {
            foreach(var v in _namespaces.Values)
            {
                yield return v;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NamespaceContainer GetNamespace(string pAlias)
        {
            return _namespaces[pAlias];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetNamespace(string pAlias, out NamespaceContainer pContainer)
        {
            if(_namespaces.ContainsKey(pAlias))
            {
                pContainer = _namespaces[pAlias];
                return true;
            }
            pContainer = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasNamespace(string pAlias)
        {
            return _namespaces.ContainsKey(pAlias);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryGetStdLib(out NamespaceContainer pContainer)
        {
            if(!string.IsNullOrEmpty(_stdlibAlias))
            {
                pContainer = _namespaces[_stdlibAlias];
                return true;
            }

            pContainer = default;
            return false;
        }
    }
}
