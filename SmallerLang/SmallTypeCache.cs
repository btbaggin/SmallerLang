using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Emitting;
using LLVMSharp;

namespace SmallerLang
{
    partial class SmallTypeCache
    {
        Dictionary<string, (SmallType Type, LLVMTypeRef LLVMType)> _cache = new Dictionary<string, (SmallType Type, LLVMTypeRef LLVMType)>();

        public string Namespace { get; private set; }

        public SmallTypeCache(string pNamespace)
        {
            Namespace = pNamespace;
        }

        internal SmallType FindType(string pType)
        {
            if (pType == null) return null;

            if (!_cache.ContainsKey(pType))
            {
                //Could be an array or generic type, parse it out to get the full type
                var t = ParseTypeString(pType);
                if (t == Undefined) return t;

                _cache[pType] = (t, default(LLVMTypeRef));
            }
            return _cache[pType].Type;
        }

        private SmallType ParseTypeString(string pType)
        {
            SmallType t = null;
            var idx = pType.LastIndexOf('[');

            if (idx > -1) t = ParseTypeString(pType.Substring(0, idx));
            else if (_primitiveTypes.ContainsKey(pType)) return _primitiveTypes[pType].Type;
            else if (_cache.ContainsKey(pType)) return _cache[pType].Type;

            if (t == null) return Undefined;
            return new SmallType(Namespace, pType, t);
        }

        internal SmallType AddEnum(string pType, string[] pFields, int[] pValues)
        {
            var st = new SmallType(Namespace, pType, pFields, pValues) { IsEnum = true };
            _cache[pType] = (st, LLVMTypeRef.Int32Type());
            return st;
        }

        internal SmallType AddStruct(string pType, FieldDefinition[] pFields)
        {
            var st = new SmallType(Namespace, pType, pFields) { IsStruct = true };
            _cache[pType] = (st, default);
            return st;
        }

        internal SmallType AddTrait(string pType, FieldDefinition[] pFields)
        {
            var st = new SmallType(Namespace, pType, pFields) { IsTrait = true };
            _cache[pType] = (st, default);
            return st;
        }

        internal void SetLLVMType(string pType, LLVMTypeRef pLLVMType)
        {
            if(_primitiveTypes.ContainsKey(pType))
            {
                _primitiveTypes[pType] = (_primitiveTypes[pType].Type, pLLVMType);
            }
            else
            {
                _cache[pType] = (_cache[pType].Type, pLLVMType);
            }
        }

        internal LLVMTypeRef GetLLVMTypeOfType(string pType)
        {
            return _cache[pType].LLVMType;
        }

        internal bool IsTypeDefinedInNamespace(string pType)
        {
            return _cache.ContainsKey(pType);
        }
    }
}
