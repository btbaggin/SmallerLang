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
    public class CompilationUnit
    {
        readonly SmallTypeCache _types;
        readonly MethodCache _methods;
        readonly Dictionary<string, CompilationModule> _references;

        public CompilationUnit()
        {
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

        internal IEnumerable<(string Alias, CompilationModule Module)> GetReferences()
        {
            foreach(var kv in _references)
            {
                yield return (kv.Key, kv.Value);
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
            _types.AddType(pType);
        }

        public void AddType(EnumSyntax pEnum)
        {
            _types.AddType(pEnum);
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
                    type = r.Value.Unit.FromString(pType);
                    if (type != SmallTypeCache.Undefined) return type;
                }
            }
            else
            {
                type = _references[pNamespace].Unit.FromString(pType);
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
            else return _references[pNamespace].Unit.IsTypeDefined(pType);
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
            return _methods.MethodExists(pType, pMethod.Name, pMethod);
        }

        public MethodDefinition AddMethod(SmallType pType, MethodSyntax pMethod)
        {
            return _methods.AddMethod(pType, pMethod.Name, pMethod);
        }

        public MethodSyntax MatchMethod(MethodCallSyntax pCall, IEnumerable<MethodSyntax> pCandidates)
        {
            return _methods.MatchMethod(pCall, pCandidates);
        }

        public IList<MethodDefinition> GetAllMatches(string pName, int pParameterCount)
        {
            return _methods.GetAllMatches(pName, pParameterCount);
        }

        public bool FindMethod(out MethodDefinition pDefinition, out bool pExact, string pNamespace, SmallType pType, string pName, params SmallType[] pArguments)
        {
            if(!string.IsNullOrEmpty(pNamespace))
            {
                return _references[pNamespace].Unit.FindMethod(out pDefinition, out pExact, "", pType, pName, pArguments);
            }
            return _methods.FindMethod(out pDefinition, out pExact, pType, pName, pArguments);
        }
        #endregion
    }
}
