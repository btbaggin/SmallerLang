using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;
using SmallerLang.Emitting;
using LLVMSharp;

namespace SmallerLang.Compiler
{
    public class CompilationCache
    {
        readonly SmallTypeCache _types;
        readonly MethodCache _methods;
        readonly Dictionary<string, CompilationModule> _references;

        public string Namespace { get; private set; }

        public CompilationCache(string pNamespace)
        {
            Namespace = pNamespace;
            _types = new SmallTypeCache();
            _methods = new MethodCache();
            _references = new Dictionary<string, CompilationModule>();
        }

        #region References
        public void AddReference(string pNamespace, CompilationModule pUnit)
        {
            _references.Add(pNamespace, pUnit);
        }

        public bool HasReference(string pAlias)
        {
            return _references.ContainsKey(pAlias);
        }

        internal CompilationModule GetReference(string pNamespace)
        {
            return _references[pNamespace];
        }

        internal IEnumerable<CompilationModule> GetAllReferences()
        {
            foreach(var r in _references.Values)
            {
                yield return r;
            }
        }

        internal int ReferenceCount()
        {
            return _references.Count;
        }
        #endregion

        #region Types
        public void AddType(TypeDefinitionSyntax pType)
        {
            _types.AddType(Namespace, pType);
        }

        public void AddType(EnumSyntax pEnum)
        {
            _types.AddType(Namespace, pEnum);
        }

        public SmallType FromString(string pType)
        {
            //Look through primitive types
            if(SmallTypeCache.TryGetPrimitive(pType, out SmallType type))
            {
                return type;
            }

            //Look for types defined in this compilation
            return _types.FindType(pType);
        }

        public SmallType FromString(NamespaceSyntax pNamespace, string pType)
        {
            if (pNamespace == null) return FromString(pType);
            else return FromStringInNamespace(pNamespace.Value, pType);
        }

        private SmallType FromStringInNamespace(string pNamespace, string pType)
        {
            SmallType type = _references[pNamespace].Cache.FromString(pType);
            if (type != SmallTypeCache.Undefined) return type;

            return FromString(pType);
        }

        public bool IsTypeDefined(string pType)
        {
            if (SmallTypeCache.IsTypeDefined(pType)) return true;
            return _types.TypeExists(pType);
        }

        public bool IsTypeDefined(NamespaceSyntax pNamespace, string pType)
        {
            if (SmallTypeCache.IsTypeDefined(pType)) return true;

            if(pNamespace == null) return IsTypeDefined(pType);
            else return _references[pNamespace.Value].Cache.IsTypeDefined(pType);
        }

        public LLVMTypeRef GetLLVMTypeOfType(string pType)
        {
            return _types.GetLLVMTypeOfType(pType);
        }

        public void SetLLVMType(string pType, LLVMTypeRef pLLVMType)
        {
            _types.SetLLVMType(pType, pLLVMType);
        }

        public SmallType MakeConcreteType(SmallType pType, params SmallType[] pGenericParameters)
        {
            if (!string.IsNullOrEmpty(pType.Namespace) && pType.Namespace != Namespace)
            {
                return _references[pType.Namespace].Cache.MakeConcreteType(pType, pGenericParameters);
            }
            return _types.GetConcreteType(pType, pGenericParameters);
        }
        #endregion

        #region Methods
        public bool MethodExists(SmallType pType, MethodSyntax pMethod)
        {
            return _methods.MethodExists(pType, pMethod);
        }

        public MethodDefinition AddMethod(SmallType pType, MethodSyntax pMethod)
        {
            return _methods.AddMethod(pType, Namespace, pMethod);
        }

        public MethodSyntax MatchMethod(MethodCallSyntax pCall, IEnumerable<MethodSyntax> pCandidates)
        {
            return _methods.MatchMethod(pCall, pCandidates);
        }

        public IList<MethodDefinition> GetAllMatches(string pNamespace, string pName, int pParameterCount)
        {
            if (!string.IsNullOrEmpty(pNamespace))
            {
                return _references[pNamespace].Cache.GetAllMatches(null, pName, pParameterCount);
            }
            return _methods.GetAllMatches(pName, pParameterCount);
        }

        public bool FindMethod(out MethodDefinition pDefinition, string pNamespace, SmallType pType, string pName, params SmallType[] pArguments)
        {
            if(!string.IsNullOrEmpty(pNamespace))
            {
                return _references[pNamespace].Cache.FindMethod(out pDefinition, "", pType, pName, pArguments);
            }
            return _methods.FindMethod(out pDefinition, pType, pName, pArguments);
        }

        public bool CastExists(SmallType pFromType, SmallType pToType, out MethodDefinition pDefinition)
        {
            foreach(var r in _references)
            {
                if (r.Value.Cache._methods.FindCast(pFromType, pToType, out pDefinition)) return true;
            }
            pDefinition = default;
            return false;
        }
        #endregion
    }
}
