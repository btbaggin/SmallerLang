using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using LLVMSharp;
using System.Reflection;
using System.Reflection.Emit;

namespace SmallerLang
{
    public sealed class SmallLangExecutionEngine : IDisposable
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int MainMethod();

        LLVMExecutionEngineRef _engine;
        readonly List<Type> _dynamicTypes;

        public SmallLangExecutionEngine()
        {
            //Need to keep a reference to the dynamic types so they don't GC while we are in SmallerLang
            _dynamicTypes = new List<Type>();
        }

        public void Run(string pPath)
        {
            try
            {
                if (LLVM.CreateMemoryBufferWithContentsOfFile(pPath, out LLVMMemoryBufferRef mem, out string message).Value != 0)
                {
                    Console.WriteLine(message);
                    return;
                }

                if (LLVM.ParseBitcode(mem, out LLVMModuleRef m, out message).Value != 0)
                {
                    Console.WriteLine(message);
                    return;
                }

                LLVM.LinkInMCJIT();
                LLVM.InitializeX86TargetMC();
                LLVM.InitializeX86Target();
                LLVM.InitializeX86TargetInfo();
                LLVM.InitializeX86AsmParser();
                LLVM.InitializeX86AsmPrinter();

                LLVMMCJITCompilerOptions options = new LLVMMCJITCompilerOptions { NoFramePointerElim = 1 };
                LLVM.InitializeMCJITCompilerOptions(options);
                if (LLVM.CreateMCJITCompilerForModule(out _engine, m, options, out message).Value != 0)
                {
                    Console.WriteLine($"Error: {message}");
                }

                SetupReversePinvokeCalls(m);

                IntPtr i = (IntPtr)LLVM.GetGlobalValueAddress(_engine, "_main");
                var main = (MainMethod)Marshal.GetDelegateForFunctionPointer(i, typeof(MainMethod));

                main();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void SetupReversePinvokeCalls(LLVMModuleRef pModule)
        {
            _dynamicTypes.Clear();
            Dictionary<IntPtr, string> mapping = new Dictionary<IntPtr, string>();
            var func = LLVM.GetFirstFunction(pModule);
            do
            {
                var attribute = LLVM.GetStringAttributeAtIndex(func, LLVMAttributeIndex.LLVMAttributeFunctionIndex, "external", 8);
                if (attribute.Pointer != IntPtr.Zero)
                {
                    //Store the assembly in a dictionary since it seems like accessing the same attribute twice doesn't work?
                    string location;
                    if (!mapping.ContainsKey(attribute.Pointer))
                    {
                        location = LLVM.GetStringAttributeValue(attribute, out uint length);
                        mapping.Add(attribute.Pointer, location);
                    }
                    else location = mapping[attribute.Pointer];
                    
                    var methodInfo = Utils.KeyAnnotations.ParseExternalAnnotation(location, func);
                    var type = CreateDynamicDelegate(methodInfo);
                    _dynamicTypes.Add(type);

                    var del = methodInfo.CreateDelegate(type);
                    LLVM.AddGlobalMapping(_engine, func, Marshal.GetFunctionPointerForDelegate(del));
                }
                func = func.GetNextFunction();
            } while (func.Pointer != IntPtr.Zero);
        }

        private Type CreateDynamicDelegate(MethodInfo pMethod)
        {
            //Create assembly
            AssemblyName name = new AssemblyName
            {
                Version = new Version(1, 0, 0, 0),
                Name = "DynamicDelegateEmit"
            };
            AssemblyBuilder assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.RunAndSave);
            ModuleBuilder module = assembly.DefineDynamicModule("DynamicDelegates", "DynamicDelegateEmit.dll");

            //Create a delegate that has the same signature as the method we would like to hook up to
            TypeBuilder type = module.DefineType("DynamicDelegateType", TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.AnsiClass | TypeAttributes.AutoClass, typeof(MulticastDelegate));
            ConstructorBuilder constructor = type.DefineConstructor(MethodAttributes.RTSpecialName | MethodAttributes.HideBySig | MethodAttributes.Public, CallingConventions.Standard, new Type[] { typeof(object), typeof(IntPtr) });
            constructor.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

            //Add the UnmanagedFunctionPointer attribute since we are calling back from SmallLang
            var con = typeof(UnmanagedFunctionPointerAttribute).GetConstructor(new Type[] { typeof(CallingConvention) });
            var cab = new CustomAttributeBuilder(con, new object[] { CallingConvention.Cdecl });
            type.SetCustomAttribute(cab);

            // Grab the parameters of the method
            ParameterInfo[] parameters = pMethod.GetParameters();
            Type[] paramTypes = new Type[parameters.Length];
            for (int i = 0; i < paramTypes.Length; i++)
            {
                paramTypes[i] = parameters[i].ParameterType;
            }

            //Define the Invoke method for the delegate
            MethodBuilder method = type.DefineMethod("Invoke", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual, pMethod.ReturnType, paramTypes);
            method.SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

            //bake it!
            Type t = type.CreateType();
            assembly.Save("DynamicDelegateEmit.dll");
            return t;
        }

    #region IDisposable Support
    private bool disposedValue = false; // To detect redundant calls
        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing) { /* No managed resources to free*/ }

                LLVM.DisposeExecutionEngine(_engine);
                disposedValue = true;
            }
        }

         ~SmallLangExecutionEngine() {
           Dispose(false);
         }

        public void Dispose()
        {
            Dispose(true);
             GC.SuppressFinalize(this);
        }
        #endregion
    }
}
