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

        public static void ValidateExternalAnnotation(string pAnnotation, MethodSyntax pMethod, IErrorReporter pError)
        {
            var parts = pAnnotation.Split(',');
            if (parts.Length != 3)
            {
                pError.WriteError("Incorrectly formatted annotation. Must be in format Assembly,Type,Method");
                return;
            }

            Assembly assembly;
            try
            {
                assembly = Assembly.Load(parts[0] + ".dll");
            }
            catch (Exception e)
            {
                pError.WriteError("Unknown assembly " + parts[0] + ": " + e.Message);
                return;
            }

            Type type;
            try
            {
                type = assembly.GetType(parts[1]);
            }
            catch (Exception e)
            {
                pError.WriteError("Unknown type " + parts[1] + ": " + e.Message);
                return;
            }

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
                else throw new Exception("Unknown type " + t.ToString());
            }

            try
            {
                type.GetMethod(parts[2], types);
            }
            catch (Exception e)
            {
                pError.WriteError("Unknown method " + parts[2] + ": " + e.Message);
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
                else throw new Exception("Unknown type " + t.ToString());
            }
            return type.GetMethod(parts[2], types);
        }
    }
}
