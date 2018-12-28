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
        internal static SmallType Boolean { get; private set; } = AddType("bool", LLVMTypeRef.Int1Type());
        internal static SmallType Char { get; private set; } = AddType("char", LLVMTypeRef.Int8Type());
        internal static SmallType String { get; private set; } = GetStringType();

        internal static SmallType GetStringType()
        {
            var length = LLVMTypeRef.Int32Type();
            var data = LLVMTypeRef.PointerType(LLVMTypeRef.Int8Type(), 0);
            var stringType = LLVMTypeRef.StructType(new LLVMTypeRef[] { length, data }, false);

            var s = new SmallType("string", Char);
            s.SetDefaultConstructor(new List<SmallType>() { new SmallType("char[]", Char) });
            _primitiveTypes["string"] = (s, stringType);
            return s;
        }

        public static SmallType FromString(string pType)
        {
            if (pType == null) return null;
            if(_primitiveTypes.ContainsKey(pType))
            {
                return _primitiveTypes[pType].Type;
            }

            return Undefined;
        }

        public static bool IsTypeDefined(string pType)
        {
            return _primitiveTypes.ContainsKey(pType);
        }

        public static string GetArrayType(string pType)
        {
            return pType + "[]";
        }

        public static string GetArrayType(SmallType pType)
        {
            return GetArrayType(pType.Name);
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

        public static SmallType CreateGenericParameter(string pType)
        {
            return CreateGenericParameter(pType, null);
        }

        public static SmallType CreateGenericParameter(string pType, SmallType pElementType)
        {
            return new SmallType(pType, pElementType) { IsGenericParameter = true };
        }

        private static SmallType AddType(string pType, LLVMTypeRef pLLVMType)
        {
            _primitiveTypes[pType] = (new SmallType(pType), pLLVMType);

            var arrType = GetArrayType(pType);
            _primitiveTypes[arrType] = (new SmallType(arrType, _primitiveTypes[pType].Type), pLLVMType);
            return _primitiveTypes[pType].Type;
        }

        internal static SmallType GetOrCreateTuple(SmallType[] pTypes)
        {
            //No return values = void
            //1 Return value = normal type
            //> 1 return values is a tuple
            if (pTypes.Length == 0) return Undefined;
            if (pTypes.Length == 1) return pTypes[0];

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

        internal static LLVMTypeRef GetLLVMType(SmallType pType)
        {
            return GetLLVMType(pType, null);
        }

        public static LLVMTypeRef GetLLVMType(SmallType pType, EmittingContext pContext)
        {
            if (pType.IsArray)
            {
                var length = LLVMTypeRef.Int32Type();
                var data = LLVMTypeRef.PointerType(GetLLVMType(pType.GetElementType(), pContext), 0);
                return LLVMTypeRef.StructType(new LLVMTypeRef[] { length, data }, false);
            }
            else if (pType.IsTuple)
            {
                LLVMTypeRef[] types = new LLVMTypeRef[pType.GetFieldCount()];
                for (int i = 0; i < types.Length; i++)
                {
                    types[i] = GetLLVMType(pType.GetFieldType(i), pContext);
                }
                return LLVMTypeRef.StructType(types, false);
            }
            else if (pType.IsGenericParameter)
            {
                System.Diagnostics.Debug.Assert(pContext.TypeMappings.ContainsKey(pType.Name));

                return GetLLVMType(pContext.TypeMappings[pType.Name], pContext);
            }
            else if (_primitiveTypes.ContainsKey(pType.Name))
            {
                return _primitiveTypes[pType.Name].LLVMType;
            }
            else
            {
                if (!string.IsNullOrEmpty(pType.Namespace))
                    return pContext.Unit.GetReference(pType.Namespace).Unit.GetLLVMTypeOfType(pType.Name);
                return pContext.Unit.GetLLVMTypeOfType(pType.Name);
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
            else if (pType.IsArray) return pContext.GetArray(pType.GetElementType(), 0, pContext);
            else if (pType.IsStruct)
            {
                var var = LLVM.BuildAlloca(pContext.Builder, GetLLVMType(pType, pContext), "struct_temp");

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
