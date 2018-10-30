using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;
using SmallerLang.Utils;
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

        internal SmallType AddType(TypeDefinitionSyntax pType)
        {
            var name = SyntaxHelper.GetFullTypeName(pType.DeclaredType);

            FieldDefinition[] fields = new FieldDefinition[pType.Fields.Count];
            for (int i = 0; i < pType.Fields.Count; i++)
            {
                var f = pType.Fields[i];
                FieldVisibility visibility = f.Annotation.Value == KeyAnnotations.Hidden ? FieldVisibility.Hidden : FieldVisibility.Public;
                fields[i] = new FieldDefinition(f.Type, f.Value, visibility);
            }

            var isStruct = pType.DefinitionType == DefinitionTypes.Struct;
            var isTrait = pType.DefinitionType == DefinitionTypes.Trait;
            var isGeneric = pType.TypeParameters.Count > 0;

            var st = new SmallType(Namespace, name, fields.ToArray()) {
                IsStruct = isStruct,
                IsTrait = isTrait,
                IsGenericType = isGeneric,
                GenericParameters = pType.TypeParameters
            };
            _cache[name] = (st, default);
            return st;
        }

        internal SmallType GetConcreteType(SmallType pType, params SmallType[] pGenericParameters)
        {
            System.Diagnostics.Debug.Assert(pType.IsGenericType);

            var name = pType.Name + "<" + string.Join<SmallType>(",", pGenericParameters) + ">";
            if(!_cache.ContainsKey(name))
            {
                var st = new SmallType(pType.Namespace, pType.Name, pType.GetFields())
                {
                    GenericArguments = pGenericParameters,
                    GenericParameters = pType.GenericParameters,
                    IsTrait = pType.IsTrait,
                    IsStruct = pType.IsStruct,
                    IsTuple = pType.IsTuple,
                    IsEnum = pType.IsEnum
                };
                st.SetConstructor(pType.GetConstructor());
                _cache[name] = (st, default);
            }

            return _cache[name].Type;
        }

        internal SmallType AddType(EnumSyntax pType)
        {
            string name = pType.Name;
            string[] fields = new string[pType.Names.Count];
            int[] values = new int[pType.Names.Count];
            for (int j = 0; j < fields.Length; j++)
            {
                fields[j] = pType.Names[j].Value;
                values[j] = pType.Values[j];
            }

            var st = new SmallType(Namespace, name, fields, values) { IsEnum = true };
            _cache[name] = (st, LLVMTypeRef.Int32Type());
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
