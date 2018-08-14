using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using LLVMSharp;

namespace SmallerLang
{
    public sealed class SmallLangExecutionEngine : IDisposable
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int MainMethod();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void print(int d);

        LLVMExecutionEngineRef _engine;

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

                var p = LLVM.GetNamedFunction(m, "print");//TODO actually call printf???
                print d = (pD) => Console.WriteLine(pD);
                LLVM.AddGlobalMapping(_engine, p, Marshal.GetFunctionPointerForDelegate(d));

                IntPtr i = (IntPtr)LLVM.GetGlobalValueAddress(_engine, "_main");
                var main = (MainMethod)Marshal.GetDelegateForFunctionPointer(i, typeof(MainMethod));
                main();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
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
