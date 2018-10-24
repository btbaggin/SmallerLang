using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using SmallerLang.Emitting;

namespace SmallerLang
{
    partial class SmallTypeCache
    {
        static Dictionary<string, (SmallType Type, LLVMTypeRef LLVMType)> _primitiveTypes = new Dictionary<string, (SmallType Type, LLVMTypeRef LLVMType)>();

        internal static SmallType Undefined { get; private set; } = AddType("undefined", LLVMTypeRef.VoidType());
        internal static SmallType Short { get; private set; } = AddType("short", LLVMTypeRef.Int16Type());
        internal static SmallType Int { get; private set; } = AddType("int", LLVMTypeRef.Int32Type());
        internal static SmallType Long { get; private set; } = AddType("long", LLVMTypeRef.Int64Type());
        internal static SmallType Float { get; private set; } = AddType("float", LLVMTypeRef.FloatType());
        internal static SmallType Double { get; private set; } = AddType("double", LLVMTypeRef.DoubleType());
        internal static SmallType String { get; private set; } = AddType("string", LLVMTypeRef.PointerType(LLVMTypeRef.Int8Type(), 0));
        internal static SmallType Boolean { get; private set; } = AddType("bool", LLVMTypeRef.Int1Type());

        public static SmallTypeCache Create(string pNamespace)
        {
            return new SmallTypeCache(pNamespace);
        }

        public static SmallType FromString(string pType)
        {
            if (pType == null) return null;
            if(_primitiveTypes.ContainsKey(pType))
            {
                return _primitiveTypes[pType].Type;
            }

            var ns = GetNamespace(ref pType);
            if(NamespaceManager.TryGetNamespace(ns, out NamespaceContainer container))
            {
                return container.FindType(pType);
            }
            return Undefined;
        }

        public static SmallType FromStringInNamespace(string pNamespace, string pType)
        {
            if (pType == null) return null;
            if (_primitiveTypes.ContainsKey(pType))
            {
                return _primitiveTypes[pType].Type;
            }

            var ns = GetNamespace(ref pType);
            if (string.IsNullOrEmpty(ns)) ns = pNamespace;

            if(NamespaceManager.TryGetNamespace(ns, out NamespaceContainer container))
            {
                return container.FindType(pType);
            }
            return Undefined;
        }

        public static bool IsTypeDefined(string pType)
        {
            if (_primitiveTypes.ContainsKey(pType)) return true;

            var ns = GetNamespace(ref pType);
            if (!NamespaceManager.TryGetNamespace(ns, out NamespaceContainer container)) return false;

            return container.IsTypeDefinedInNamespace(pType);
        }

        internal static string GetNamespace(ref string pType)
        {
            var idx = pType.IndexOf('.');
            if(idx > -1)
            {
                var ns = pType.Substring(0, idx);
                pType = pType.Substring(idx + 1);
                return ns;
            }

            return "";
        }

        private static SmallType AddType(string pType, LLVMTypeRef pLLVMType)
        {
            _primitiveTypes[pType] = (new SmallType("", pType), pLLVMType);
            return _primitiveTypes[pType].Type;
        }

        internal static SmallType GetOrCreateTuple(SmallType[] pTypes)
        {
            StringBuilder sb = new StringBuilder("!!");
            foreach (var t in pTypes)
            {
                sb.Append(t.Name + "_");
            }

            var name = sb.ToString();
            if (!_primitiveTypes.ContainsKey(name))
            {
                //We don't need to populate field names because we only ever access tuple fields by index
                FieldDefinition[] fields = new FieldDefinition[pTypes.Length];
                for (int i = 0; i < fields.Length; i++)
                {
                    fields[i] = new FieldDefinition(pTypes[i], "");
                }

                var st = new SmallType("", name, fields) { IsTuple = true };
                _primitiveTypes[name] = (st, default);
            }

            return _primitiveTypes[name].Type;
        }

        internal static void SetLLVMType(string pNamespace, string pType, LLVMTypeRef pLLVMType)
        {
            System.Diagnostics.Debug.Assert(NamespaceManager.TryGetNamespace(pNamespace, out NamespaceContainer container));
            container.SetLLVMType(pType, pLLVMType);
        }

        public static LLVMTypeRef GetLLVMType(SmallType pType)
        {
            if (pType.IsArray)
            {
                var length = LLVMTypeRef.Int32Type();
                var data = LLVMTypeRef.PointerType(GetLLVMType(pType.GetElementType()), 0);
                return LLVMTypeRef.StructType(new LLVMTypeRef[] { length, data }, false);
            }
            else if (pType.IsTuple)
            {
                LLVMTypeRef[] types = new LLVMTypeRef[pType.GetFieldCount()];
                for (int i = 0; i < types.Length; i++)
                {
                    types[i] = GetLLVMType(pType.GetFieldType(i));
                }
                return LLVMTypeRef.StructType(types, false);
            }
            else if (_primitiveTypes.ContainsKey(pType.Name))
            {
                return _primitiveTypes[pType.Name].LLVMType;
            }
            else if (NamespaceManager.TryGetNamespace(pType.Namespace, out NamespaceContainer container))
            {
                 return container.GetLLVMTypeOfType(pType.Name);
            }

            throw new ArgumentException("Unable to convert to LLVM type " + pType.ToString());
        }

        public static LLVMValueRef GetLLVMDefault(SmallType pType, EmittingContext pContext)
        {
            if (pType == Boolean) return pContext.GetInt1(0);
            else if (pType == Int) return pContext.GetInt(0);
            else if (pType == Short) return pContext.GetShort(0);
            else if (pType == Long) return pContext.GetLong(0);
            else if (pType == Float) return pContext.GetFloat(0);
            else if (pType == Double) return pContext.GetDouble(0);
            else if (pType == String) return pContext.GetString(null);
            else if (pType.IsArray) return pContext.GetArray(pType.GetElementType(), 0);
            else if (pType.IsStruct)
            {
                var var = LLVM.BuildAlloca(pContext.Builder, GetLLVMType(pType), "struct_temp");

                var m = pType.GetConstructor();
                LLVMValueRef[] arguments = new LLVMValueRef[m.ArgumentTypes.Count + 1];
                arguments[0] = var;
                for (int i = 0; i < m.ArgumentTypes.Count; i++)
                {
                    arguments[i + 1] = GetLLVMDefault(m.ArgumentTypes[i], pContext);
                }
                LLVM.BuildCall(pContext.Builder, pContext.GetMethod(m.MangledName), arguments, "");
                return LLVM.BuildLoad(pContext.Builder, var, "");
            }

            throw new ArgumentException("Unknown type " + pType.ToString());
        }
    }
}
