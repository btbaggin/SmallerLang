using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using LLVMSharp;
using SmallerLang.Syntax;
using cache = SmallerLang.SmallTypeCache;

namespace SmallerLang.Utils
{
    public static class KeyAnnotations
    {
        public static string Constructor => "new";
        public static string RunMethod => "run";
        public static string Complete => "complete";
        public static string Hidden => "hidden";

        public static void ValidateExternalAnnotation(Annotation pAnnotation, MethodSyntax pMethod)
        {
            //Check basic format
            if(string.IsNullOrEmpty(pAnnotation.Value))
            {
                CompilerErrors.NoExternalAnnotation(pMethod.Span);
                return;
            }

            var parts = pAnnotation.Value.Split(',');
            if (parts.Length != 3)
            {
                CompilerErrors.ExternalAnnotationFormat(pAnnotation.Span);
                return;
            }

            try
            {
                //Try to retrieve assembly
                Assembly assembly = TryResolveAssembly(parts[0]);
                if (assembly == null) throw new System.IO.FileNotFoundException($"Unable to locate assembly {parts[0]}");

                //Try to retrieve type
                Type type = assembly.GetType(parts[1]);
                if (type == null) throw new System.IO.FileNotFoundException($"Unable to type {parts[1]} within {parts[0]}");

                //Convert SmallTypes to System.Type
                Type[] types = new Type[pMethod.Parameters.Count];
                for (int i = 0; i < pMethod.Parameters.Count; i++)
                {
                    var t = pMethod.Parameters[i].Type;
                    if (t == cache.Double) types[i] = typeof(double);
                    else if (t == cache.Float) types[i] = typeof(float);
                    else if (t == cache.Long) types[i] = typeof(long);
                    else if (t == cache.Int) types[i] = typeof(int);
                    else if (t == cache.Short) types[i] = typeof(short);
                    else if (t == cache.Boolean) types[i] = typeof(bool);
                    else if (t == cache.String) types[i] = typeof(string);
                    else if (t == cache.Char) types[i] = typeof(char);
                    else if (t.IsEnum) types[i] = typeof(Enum);
                    else throw new InvalidCastException("Unknown type " + t.ToString());
                }

                if (!TryResolveMethod(type, parts[2], types, out MethodInfo method))
                {
                    CompilerErrors.UnknownExternalMethod(parts[2], pAnnotation.Span);
                }
                else
                {
                    //Method must be a static and double check argument types. Primitive types can be implicitly casted in GetMethod
                    if (!method.IsStatic)
                    {
                        CompilerErrors.StaticExternalMethod(parts[2], pAnnotation.Span);
                    }

                    var parmTypes = method.GetParameters();
                    for (int i = 0; i < parmTypes.Length; i++)
                    {
                        //We will allow use of SmallType Enums to System.Enum
                        //This works because we just emit the integer value
                        if (parmTypes[i].ParameterType != types[i] &&
                            parmTypes[i].ParameterType.IsEnum && types[i] != typeof(Enum))
                        {
                            CompilerErrors.TypeCastError(types[i].ToString(), parmTypes[i].ParameterType.ToString(), pAnnotation.Span);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                CompilerErrors.UnknownExternalError(e.Message, pAnnotation.Span);
            }
        }

        public static MethodInfo ParseExternalAnnotation(string pAnnotation, LLVMValueRef pFunction)
        {
            var parts = pAnnotation.Split(',');
            Assembly assembly = TryResolveAssembly(parts[0]);

            Type type = assembly.GetType(parts[1]);

            //Get parameter types so we can get the correct overload
            var parms = pFunction.GetParams();
            Type[] types = new Type[parms.Length];
            for (int i = 0; i < parms.Length; i++)
            {
                var t = parms[i].TypeOf();
                if (t.Equals(cache.GetLLVMType(cache.Double))) types[i] = typeof(double);
                else if (t.Equals(cache.GetLLVMType(cache.Float))) types[i] = typeof(float);
                else if (t.Equals(cache.GetLLVMType(cache.Long))) types[i] = typeof(long);
                else if (t.Equals(cache.GetLLVMType(cache.Int))) types[i] = typeof(int);
                else if (t.Equals(cache.GetLLVMType(cache.Short))) types[i] = typeof(short);
                else if (t.Equals(cache.GetLLVMType(cache.Boolean))) types[i] = typeof(bool);
                else if (t.Equals(LLVM.PointerType(LLVM.Int8Type(), 0))) types[i] = typeof(string); //For strings we are only passing the null terminated char pointer
                else if (t.Equals(cache.GetLLVMType(cache.Char))) types[i] = typeof(char);
                else throw new InvalidCastException("Unknown type " + t.ToString());
            }
            bool resolved = TryResolveMethod(type, parts[2], types, out MethodInfo m);
            System.Diagnostics.Debug.Assert(resolved);
            return m;
        }

        private static Assembly TryResolveAssembly(string pAssembly)
        {
            pAssembly = pAssembly + ".dll";
            //Try just straight load
            Assembly assembly = null;
            try
            {
                assembly = Assembly.Load(pAssembly);
            }
            catch(System.IO.FileNotFoundException)
            {
                //Try loading from the .Net directory
                AssemblyName name;
                string path;
                try
                {
                    path = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
                    name = AssemblyName.GetAssemblyName(System.IO.Path.Combine(path, pAssembly));
                    assembly = Assembly.Load(name);
                }
                catch(System.IO.FileNotFoundException)
                {
                    //Try loading from the reference assembly directory
                    try
                    {
                        path = Microsoft.Build.Utilities.ToolLocationHelper.GetPathToDotNetFrameworkReferenceAssemblies(Microsoft.Build.Utilities.TargetDotNetFrameworkVersion.VersionLatest);// @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5.1\";
                        name = AssemblyName.GetAssemblyName(System.IO.Path.Combine(path, pAssembly));
                        assembly = Assembly.Load(name);
                    }
                    catch
                    {
                        //Try loading from compilation directory
                        try
                        {
                            path = Compiler.SmallCompiler.CurrentDirectory;
                            if (!string.IsNullOrEmpty(path))
                            {
                                name = AssemblyName.GetAssemblyName(System.IO.Path.Combine(path, pAssembly));
                                assembly = Assembly.Load(name);
                            }
                        }
                        catch (Exception)
                        {
                            //We have tried all possibilities, just return a null assembly and let the calling function handle it
                        }
                    }
                }
            }

            return assembly;
        }

        private static bool TryResolveMethod(Type pType, string pName, Type[] pArguments, out MethodInfo pMethod)
        {
            pMethod = pType.GetMethod(pName, pArguments);
            if (pMethod != null) return true;

            pMethod = pType.GetMethod(pName);
            if(pMethod != null) return true;

            return false;
        }
    }
}
