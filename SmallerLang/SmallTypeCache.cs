using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Emitting;
using LLVMSharp;

namespace SmallerLang
{
    static class SmallTypeCache
    {
        internal static SmallType Undefined { get; private set; }
        internal static SmallType Short { get; private set; }   
        internal static SmallType Int { get; private set; } 
        internal static SmallType Long { get; private set; } 
        internal static SmallType Float { get; private set; } 
        internal static SmallType Double { get; private set; } 
        internal static SmallType String { get; private set; } 
        internal static SmallType Boolean { get; private set; } 

        static Dictionary<string, (SmallType Type, LLVMTypeRef LLVMType)> _cache;
        static SmallTypeCache()
        {
            _cache = new Dictionary<string, (SmallType, LLVMTypeRef)>();
            Undefined = AddType("undefined", LLVMTypeRef.VoidType());
            Short = AddType("short", LLVMTypeRef.Int16Type());
            Int = AddType("int", LLVMTypeRef.Int32Type());
            Long = AddType("long", LLVMTypeRef.Int64Type());
            Float = AddType("float", LLVMTypeRef.FloatType());
            Double = AddType("double", LLVMTypeRef.DoubleType());
            String = AddType("string", LLVMTypeRef.PointerType(LLVMTypeRef.Int8Type(), 0));
            Boolean = AddType("bool", LLVMTypeRef.Int1Type());
        }

        public static SmallType FromString(string pType)
        {
            if (pType == null) return null;

            if (!_cache.ContainsKey(pType))
            {
                //Should only hit this for arrays... cache the array type
                var t = ParseTypeString(pType);
                if (t == Undefined) return t;

                _cache[pType] = (t, default(LLVMTypeRef));
            }
            return _cache[pType].Type;
        }

        internal static SmallType AddEnum(string pType, string[] pFields, int[] pValues)
        {
            var st = new SmallType(pType, pFields, pValues);
            _cache[pType] = (st, LLVMTypeRef.Int32Type());
            return st;
        }

        internal static SmallType AddStruct(string pType, string[] pFields, SmallType[] pTypes)
        {
            System.Diagnostics.Debug.Assert(pFields.Length == pTypes.Length, "Incorrect field definitions");

            //var baseType = FromString(pInherits);
            var st = new SmallType(pType, pFields, pTypes);
            _cache[pType] = (st, default);
            return st;
        }

        internal static SmallType AddTrait(string pType, string[] pFields, SmallType[] pTypes)
        {
            System.Diagnostics.Debug.Assert(pFields.Length == pTypes.Length, "Incorrect field definitions");

            var st = new SmallType(pType, pFields, pTypes) { IsTrait = true };
            _cache[pType] = (st, default);
            return st;
        }

        internal static SmallType GetTempTuple(SmallType[] pTypes)
        {
            StringBuilder sb = new StringBuilder("!!");
            foreach(var t in pTypes)
            {
                sb.Append(t.Name + "_");
            }

            var name = sb.ToString();
            if(!_cache.ContainsKey(name))
            {
                //We don't need to populate field names because we only ever access tuple fields by index
                string[] fieldNames = new string[pTypes.Length];
                
                var st = new SmallType(name, fieldNames, pTypes) { IsTuple = true };
                _cache[name] = (st, default);
            }
            
            return _cache[name].Type;
        }

        internal static void SetLLVMType(string pType, LLVMTypeRef pLLVMType)
        {
            _cache[pType] = (_cache[pType].Type, pLLVMType);
        }

        private static SmallType AddType(string pType, LLVMTypeRef pLLVMType)
        {
            _cache[pType] = (new SmallType(pType), pLLVMType);
            return _cache[pType].Type;
        }

        private static SmallType ParseTypeString(string pType)
        {
            SmallType t = null;
            var i = pType.LastIndexOf('[');

            if (i > -1) t = ParseTypeString(pType.Substring(0, i));
            else if (_cache.ContainsKey(pType)) return _cache[pType].Type;

            if (t == null) return Undefined;
            return new SmallType(pType, t);
        }

        public static bool IsTypeDefined(string pType)
        {
            return _cache.ContainsKey(pType);
        }

        public static LLVMTypeRef GetLLVMType(SmallType pType)
        {
            if (pType.IsArray)
            {
                var length = LLVMTypeRef.Int32Type();
                var data = LLVMTypeRef.PointerType(GetLLVMType(pType.GetElementType()), 0);
                return LLVMTypeRef.StructType(new LLVMTypeRef[] { length, data }, false);
            }
            else if (_cache.ContainsKey(pType.Name))
            {
                return _cache[pType.Name].LLVMType;
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
