﻿using System;
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
            var type = SmallTypeCache.FromString(pType);

            //Look for types defined in this compilation
            if (type == SmallTypeCache.Undefined)
            {
                type = _types.FindType(pType);
            }
            return type;
        }

        internal SmallType FromStringInNamespace(string pNamespace, string pType)
        {
            SmallType type;
            if(pNamespace == null)
            {
                foreach (var r in _references)
                {
                    type = r.Value.Cache.FromString(pType);
                    if (type != SmallTypeCache.Undefined) return type;
                }
            }
            else
            {
                type = _references[pNamespace].Cache.FromString(pType);
                if (type != SmallTypeCache.Undefined) return type;
            }

            return FromString(pType);
        }

        public bool IsTypeDefined(string pType)
        {
            if (SmallTypeCache.IsTypeDefined(pType)) return true;
            return _types.TypeExists(pType);
        }

        public bool IsTypeDefined(string pNamespace, string pType)
        {
            if (SmallTypeCache.IsTypeDefined(pType)) return true;

            if(string.IsNullOrEmpty(pNamespace)) return IsTypeDefined(pType);
            else return _references[pNamespace].Cache.IsTypeDefined(pType);
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

        public IList<MethodDefinition> GetAllMatches(string pName, int pParameterCount)
        {
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
