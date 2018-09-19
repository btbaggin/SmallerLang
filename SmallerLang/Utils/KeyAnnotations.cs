using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using LLVMSharp;
using SmallerLang.Syntax;

namespace SmallerLang.Utils
{
    public static class KeyAnnotations
    {
        public static string Constructor => "new";
        public static string RunMethod => "run";
        public static string Complete => "complete";
        public static string Hidden => "hidden";

        public static void ValidateExternalAnnotation(Annotation pAnnotation, MethodSyntax pMethod, IErrorReporter pError)
        {
            //Check basic format
            var parts = pAnnotation.Value.Split(',');
            if (parts.Length != 3)
            {
                pError.WriteError("Incorrectly formatted annotation. Must be in format Assembly,Type,Method", pAnnotation.Span);
                return;
            }

            try
            {
                //Try to retrieve assembly
                Assembly assembly = Assembly.Load(parts[0] + ".dll");

                //Try to retrieve type
                Type type = assembly.GetType(parts[1]);

                //Convert SmallTypes to System.Type
                Type[] types = new Type[pMethod.Parameters.Count];
                for (int i = 0; i < pMethod.Parameters.Count; i++)
                {
                    var t = pMethod.Parameters[i].Type;
                    if (t == SmallTypeCache.Double) types[i] = typeof(double);
                    else if (t == SmallTypeCache.Float) types[i] = typeof(float);
                    else if (t == SmallTypeCache.Long) types[i] = typeof(long);
                    else if (t == SmallTypeCache.Int) types[i] = typeof(int);
                    else if (t == SmallTypeCache.Short) types[i] = typeof(short);
                    else if (t == SmallTypeCache.Boolean) types[i] = typeof(bool);
                    else throw new InvalidCastException("Unknown type " + t.ToString());
                }

                MethodInfo method = type.GetMethod(parts[2], types);

                if (method == null) pError.WriteError("Unknown method " + parts[2], pAnnotation.Span);
                else
                {
                    //Method must be a static and double check argument types. Primitive types can be implicitly casted in GetMethod
                    if (!method.IsStatic)
                    {
                        pError.WriteError("Method must be static", pAnnotation.Span);
                    }

                    var parmTypes = method.GetParameters();
                    for (int i = 0; i < parmTypes.Length; i++)
                    {
                        if (parmTypes[i].ParameterType != types[i])
                        {
                            pError.WriteError($"Cannot convert type {types[i]} to {parmTypes[i].ParameterType}", pAnnotation.Span);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                pError.WriteError("Error validating external method: " + e.Message, pAnnotation.Span);
            }
        }

        public static MethodInfo ParseExternalAnnotation(string pAnnotation, LLVMValueRef pFunction)
        {
            var parts = pAnnotation.Split(',');
            Assembly assembly = Assembly.Load(parts[0] + ".dll");

            Type type = assembly.GetType(parts[1]);

            //Get parameter types so we can get the correct overload
            var parms = pFunction.GetParams();
            Type[] types = new Type[parms.Length];
            for (int i = 0; i < parms.Length; i++)
            {
                var t = parms[i].TypeOf();
                if(t.Equals(LLVMTypeRef.DoubleType())) types[i] = typeof(double);
                else if (t.Equals(LLVMTypeRef.FloatType())) types[i] = typeof(float);
                else if (t.Equals(LLVMTypeRef.Int64Type())) types[i] = typeof(long);
                else if (t.Equals(LLVMTypeRef.Int32Type())) types[i] = typeof(int);
                else if (t.Equals(LLVMTypeRef.Int16Type())) types[i] = typeof(short);
                else if (t.Equals(LLVMTypeRef.Int1Type())) types[i] = typeof(bool);
                else throw new InvalidCastException("Unknown type " + t.ToString());
            }
            return type.GetMethod(parts[2], types);
        }
    }
}
