﻿using System;
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
        static Dictionary<string, (SmallType Type, LLVMTypeRef LLVMType)> _cache = new Dictionary<string, (SmallType Type, LLVMTypeRef LLVMType)>();

        internal static SmallType Undefined { get; private set; } = AddType("undefined", LLVMTypeRef.VoidType());
        internal static SmallType Short { get; private set; } = AddType("short", LLVMTypeRef.Int16Type());
        internal static SmallType Int { get; private set; } = AddType("int", LLVMTypeRef.Int32Type());
        internal static SmallType Long { get; private set; } = AddType("long", LLVMTypeRef.Int64Type());
        internal static SmallType Float { get; private set; } = AddType("float", LLVMTypeRef.FloatType());
        internal static SmallType Double { get; private set; } = AddType("double", LLVMTypeRef.DoubleType());
        internal static SmallType String { get; private set; } = AddType("string", LLVMTypeRef.PointerType(LLVMTypeRef.Int8Type(), 0));
        internal static SmallType Boolean { get; private set; } = AddType("bool", LLVMTypeRef.Int1Type());

        public static SmallType FromString(string pType)
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

        internal static SmallType AddEnum(string pType, string[] pFields, int[] pValues)
        {
            var st = new SmallType(pType, pFields, pValues) { IsEnum = true };
            _cache[pType] = (st, LLVMTypeRef.Int32Type());
            return st;
        }

        internal static SmallType AddStruct(string pType, FieldDefinition[] pFields)
        {
            var st = new SmallType(pType, pFields) { IsStruct = true };
            _cache[pType] = (st, default);
            return st;
        }

        internal static SmallType AddTrait(string pType, FieldDefinition[] pFields)
        {
            var st = new SmallType(pType, pFields) { IsTrait = true };
            _cache[pType] = (st, default);
            return st;
        }

        internal static SmallType GetOrCreateTuple(SmallType[] pTypes)
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
                FieldDefinition[] fields = new FieldDefinition[pTypes.Length];
                for(int i = 0; i < fields.Length; i++)
                {
                    fields[i] = new FieldDefinition(pTypes[i], "");
                }
                
                var st = new SmallType(name, fields) { IsTuple = true };
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
            var idx = pType.LastIndexOf('[');

            if (idx > -1) t = ParseTypeString(pType.Substring(0, idx));
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
