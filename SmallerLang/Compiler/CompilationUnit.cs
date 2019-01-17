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
    public enum FindResult
    {
        Found,
        IncorrectScope,
        NotFound
    }

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

        public FindResult FromString(string pName, out SmallType pType)
        {
            //Look through primitive types
            if(SmallTypeCache.TryGetPrimitive(pName, out pType))
            {
                return FindResult.Found;
            }

            //Look for types defined in this compilation
            pType = _types.FindType(pName);
            if (pType == SmallTypeCache.Undefined) return FindResult.NotFound;
            return FindResult.Found;
        }

        public FindResult FromString(TypeSyntax pNode, out SmallType pType)
        {
            var name = Utils.SyntaxHelper.GetFullTypeName(pNode);
            if (pNode.Namespace == null)
            {
                return FromString(name, out pType);
            }
            else
            {
                var result  = _references[pNode.Namespace.Value].Cache.FromString(name, out pType);
                if (result == FindResult.NotFound)
                {
                    return FromString(name, out pType);
                }
                else if(pType.Scope == FileScope.Private)
                {
                    return FindResult.IncorrectScope;
                }
                return result;
            }
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
        public FindResult MethodExists(SmallType pType, MethodSyntax pMethod)
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

        public FindResult FindMethod(out MethodDefinition pDefinition, string pNamespace, SmallType pType, string pName, params SmallType[] pArguments)
        {
            if(!string.IsNullOrEmpty(pNamespace))
            {
                //If we are finding a method in a module we are not going to allow finding private methods
                return _references[pNamespace].Cache._methods.FindMethod(out pDefinition, false, pType, pName, pArguments);
            }

            //This is in the curret module, anything is fair game
            return _methods.FindMethod(out pDefinition, true, pType, pName, pArguments);
        }

        public FindResult CastExists(SmallType pFromType, SmallType pToType, out MethodDefinition pDefinition)
        {
            foreach(var r in _references)
            {
                if (r.Value.Cache._methods.FindCast(pFromType, pToType, out pDefinition)) return FindResult.Found;
            }
            pDefinition = default;
            return FindResult.NotFound;
        }
        #endregion
    }
}
