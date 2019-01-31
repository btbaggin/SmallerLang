using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;
using SmallerLang.Utils;
using SmallerLang.Emitting;
using LLVMSharp;

namespace SmallerLang
{
    partial class SmallTypeCache
    {
        Dictionary<string, (SmallType Type, LLVMTypeRef LLVMType)> _cache = new Dictionary<string, (SmallType Type, LLVMTypeRef LLVMType)>();

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

            if (t == null || t == Undefined) return Undefined;
            return new SmallType(pType, t);
        }

        internal SmallType AddType(string pNamespace, TypeDefinitionSyntax pType, List<TypeDefinitionSyntax> pImplements)
        {
            var name = SyntaxHelper.GetFullTypeName(pType.DeclaredType);

            List<FieldDefinition> fields = new List<FieldDefinition>(pType.Fields.Count);
            HashSet<string> fieldNames = new HashSet<string>();
            //Add fields from base struct
            foreach(var f in pType.Fields)
            {
                FieldVisibility visibility = KeyAnnotations.GetVisibility(f);
                fields.Add(new FieldDefinition(f.Type, f.Value, visibility));

                //Check for duplicate field names
                if(!fieldNames.Add(f.Value))
                {
                    CompilerErrors.DuplicateField(f.Value, pType, f.Span);
                }
            }

            if(pImplements != null)
            {
                //Add fields from all implements
                foreach(var i in pImplements)
                {
                    foreach(var f in i.Fields)
                    {
                        //Implement fields only get added once to the base struct
                        if(fieldNames.Add(f.Value))
                        {
                            FieldVisibility visibility = KeyAnnotations.GetVisibility(f);
                            fields.Add(new FieldDefinition(f.Type, f.Value, visibility));
                        }
                    }
                }
            }

            var isStruct = pType.DefinitionType == DefinitionTypes.Struct;
            var isTrait = pType.DefinitionType == DefinitionTypes.Trait;
            var isImpl = pType.DefinitionType == DefinitionTypes.Implement;
            var isGeneric = pType.TypeParameters.Count > 0;

            var st = new SmallType(pNamespace, name, fields.ToArray()) {
                IsStruct = isStruct,
                IsTrait = isTrait,
                IsImpl = isImpl,
                IsGenericType = isGeneric,
                GenericParameters = pType.TypeParameters,
                Scope = pType.Scope
            };
            _cache[name] = (st, default);
            return st;
        }

        internal static string GetConcreteTypeName(string pTypeName, params SmallType[] pGenericParameters)
        {
            return pTypeName + "<" + string.Join<SmallType>(",", pGenericParameters) + ">";
        }

        internal SmallType GetConcreteType(SmallType pType, params SmallType[] pGenericParameters)
        {
            System.Diagnostics.Debug.Assert(pType.IsGenericType);

            var name = GetConcreteTypeName(pType.Name, pGenericParameters);
            if(!_cache.ContainsKey(name))
            {
                List<FieldDefinition> fields = new List<FieldDefinition>();

                //Create a mapping of type parameters to index
                Dictionary<string, int> parmMapping = new Dictionary<string, int>();
                for(int i = 0; i < pType.GenericParameters.Count; i++)
                {
                    parmMapping.Add(pType.GenericParameters[i], i);
                }

                //Poly the fields
                foreach(var f in pType.GetFields())
                {
                    SmallType newFieldType = f.Type;
                    if(f.Type.IsGenericType)
                    {
                        newFieldType = GetConcreteType(f.Type, pGenericParameters);
                    }
                    else if(f.Type.IsGenericParameter)
                    {
                        newFieldType = pGenericParameters[parmMapping[f.Type.Name]];
                    }
                    fields.Add(new FieldDefinition(newFieldType, f.Name, f.Visibility));
                }

                var st = new SmallType(pType.Namespace, pType.Name, fields.ToArray())
                {
                    GenericArguments = pGenericParameters,
                    GenericParameters = pType.GenericParameters,
                    IsTrait = pType.IsTrait,
                    IsStruct = pType.IsStruct,
                    IsTuple = pType.IsTuple,
                    IsEnum = pType.IsEnum,
                };

                //Poly constructor
                if(pType.HasDefinedConstructor())
                {
                    var existingCtor = pType.GetConstructor();
                    List<SmallType> argumentTypes = new List<SmallType>();
                    foreach (var t in existingCtor.ArgumentTypes)
                    {
                        var argType = t;
                        if (t.IsGenericType)
                        {
                            GetConcreteType(t, pGenericParameters);
                        }
                        else if (t.IsGenericParameter)
                        {
                            argType = pGenericParameters[parmMapping[t.Name]];
                        }
                        argumentTypes.Add(argType);
                    }

                    var ctor = new MethodDefinition(FileScope.Public, existingCtor.Name, existingCtor.MangledName, false, argumentTypes, Undefined);
                    st.SetConstructor(ctor, pType.HasDefinedConstructor());
                }
                else
                {
                    st.SetDefaultConstructor();
                }
               

                foreach(var i in pType.Implements)
                {
                    st.AddTrait(i);
                }

                _cache[name] = (st, default);
            }

            return _cache[name].Type;
        }

        internal SmallType AddType(string pNamespace, EnumSyntax pType)
        {
            string name = pType.Name;
            string[] fields = new string[pType.Names.Count];
            int[] values = new int[pType.Names.Count];
            for (int j = 0; j < fields.Length; j++)
            {
                fields[j] = pType.Names[j].Value;
                values[j] = pType.Values[j];
            }

            var st = new SmallType(pNamespace, name, fields, values) {
                IsEnum = true,
                Scope = pType.Scope
            };
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

        public bool TypeExists(string pType)
        {
            return _cache.ContainsKey(pType);
        }
    }
}
