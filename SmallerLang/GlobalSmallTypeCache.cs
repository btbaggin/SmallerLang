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

        //Undefined is used when a type is not yet known or not defined within the source file
        //NoValue is used to say a certain expression does not produce a value
        internal static SmallType Undefined { get; private set; } = AddType("undefined", LLVMTypeRef.VoidType());
        internal static SmallType NoValue { get; private set; } = AddType("__novalue__", LLVMTypeRef.VoidType());

        internal static SmallType Short { get; private set; } = AddType("short", LLVMTypeRef.Int16Type());
        internal static SmallType Int { get; private set; } = AddType("int", LLVMTypeRef.Int32Type());
        internal static SmallType Long { get; private set; } = AddType("long", LLVMTypeRef.Int64Type());
        internal static SmallType Float { get; private set; } = AddType("float", LLVMTypeRef.FloatType());
        internal static SmallType Double { get; private set; } = AddType("double", LLVMTypeRef.DoubleType());
        internal static SmallType Boolean { get; private set; } = AddType("bool", LLVMTypeRef.Int1Type());
        internal static SmallType Char { get; private set; } = AddType("char", LLVMTypeRef.Int8Type());
        internal static SmallType String { get; private set; } = GetStringType();

        private static SmallType GetStringType()
        {
            var length = LLVMTypeRef.Int32Type();
            var data = LLVMTypeRef.PointerType(LLVMTypeRef.Int8Type(), 0);
            var stringType = LLVMTypeRef.StructType(new LLVMTypeRef[] { length, data }, false);

            var s = new SmallType("string", Char);
            s.SetDefaultConstructor(new List<SmallType>() { new SmallType("char[]", Char) });
            _primitiveTypes["string"] = (s, stringType);
            return s;
        }

        #region TryGet standard types
        internal static bool TryGetEnumerable(Compiler.CompilationCache pCache, out SmallType pType)
        {
            return TryGetStandardType(pCache, "Enumerable`1", out pType);
        }

        internal static bool TryGetDisposable(Compiler.CompilationCache pCache, out SmallType pType)
        {
            return TryGetStandardType(pCache, "Disposable", out pType);
        }

        private static bool TryGetStandardType(Compiler.CompilationCache pCache, string pTypeName, out SmallType pType)
        {
            foreach (var r in pCache.GetAllReferences())
            {
                var result = r.Cache.FromString(pTypeName, out pType);
                if (result == Compiler.FindResult.Found) return true;
            }
            pType = Undefined;
            return false;
        }
        #endregion

        public static bool TryGetPrimitive(string pName, out SmallType pType)
        {
            if (pName == null)
            {
                pType = null;
                return false;
            }

            if(_primitiveTypes.ContainsKey(pName))
            {
                pType = _primitiveTypes[pName].Type;
                return true;
            }

            pType = Undefined;
            return false;
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
            if (pTypes.Length == 0) return NoValue;
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

        /// <summary>
        /// Retrieves the LLVM type from the given SmallType without a context
        /// Should only be used to retrieve primitive types
        /// </summary>
        /// <param name="pType">The type for which to retrieve the corresponding LLVMTypeRef</param>
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
                //Tuples will be composed of all 
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

                //Generic parameters are retrieved from the type mappings of the current struct
                return GetLLVMType(pContext.TypeMappings[pType.Name], pContext);
            }
            else if (_primitiveTypes.ContainsKey(pType.Name))
            {
                return _primitiveTypes[pType.Name].LLVMType;
            }
            else
            {
                //Only get the namespace if it's different than the one we are currently in
                if (!string.IsNullOrEmpty(pType.Namespace) && 
                    pType.Namespace != pContext.Cache.Namespace)
                {
                    return pContext.Cache.GetReference(pType.Namespace).Cache.GetLLVMTypeOfType(pType.GetFullName());
                }

                //Otherwise type should be defined in the current namespace
                return pContext.Cache.GetLLVMTypeOfType(pType.GetFullName());
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
            else if (pType == Char) return LLVM.ConstInt(LLVM.Int8Type(), 0, EmittingContext.False);
            else if (pType == String)
            {
                //Allocate space for our string
                LLVMValueRef variable;
                LLVMValueRef data;
                using (var b = new VariableDeclarationBuilder(pContext))
                {
                    variable = LLVM.BuildAlloca(b.Builder, GetLLVMType(String, pContext), "string_temp");
                    data = LLVM.BuildAlloca(b.Builder, LLVM.PointerType(LLVMTypeRef.Int8Type(), 0), "");
                }

                //Save length
                var length = pContext.GetArrayLength(variable);
                LLVM.BuildStore(pContext.Builder, pContext.GetInt(0), length);

                //Store the string constant in the allocated array
                data = LLVM.BuildLoad(pContext.Builder, data, "");

                //Store the allocated array in the string variable
                var variableData = LLVM.BuildInBoundsGEP(pContext.Builder, variable, new LLVMValueRef[] { pContext.GetInt(0), pContext.GetInt(1) }, "");
                LLVM.BuildStore(pContext.Builder, data, variableData);

                variable = LLVM.BuildLoad(pContext.Builder, variable, "");
                return variable;
            }
            else if (pType.IsArray) return pContext.GetArray(pType.GetElementType(), 0, pContext);
            else if (pType.IsStruct)
            {
                LLVMValueRef var;
                using (var b = new VariableDeclarationBuilder(pContext))
                {
                    var = LLVM.BuildAlloca(b.Builder, GetLLVMType(pType, pContext), "struct_temp");
                }

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
            else if (pType.IsGenericParameter)
            {
                System.Diagnostics.Debug.Assert(pContext.TypeMappings.ContainsKey(pType.Name));

                return GetLLVMDefault(pContext.TypeMappings[pType.Name], pContext);
            }

            throw new ArgumentException("Unknown type " + pType.ToString());
        }
    }
}
