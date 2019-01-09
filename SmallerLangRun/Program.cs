using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang;
using SmallerLang.Compiler;

namespace SmallerLangRun
{ 
    static class Program
    {
        /*TODO
         * Hide methods/types in modules
         * returning arrays not working
         * constructor overloads
         * Look to make some structs ref returns
         * remove unused structs
         * Allow multiple modules to the same namespace alias
         * Contracts???
         * 
         * Range 1..10
         * Zero out array allocations
         * Allow returning traits from methods? - No, this should be an error
         * tree shaking of methods
         * control flow analysis?
        */
        static void Main(string[] args)
        {
            var prog = @"C:\Test\SML\simple.sml";
            var output = @"C:\Test\SML\test.bc";
            var c = new SmallCompiler();
            var o = new CompilerOptions(output)
            {
                OutputBytecode = false,
                Optimizations = true,
                SourceFile = prog,
                Debug = false
            };

            if (c.Compile(o))
            {
                var e = new SmallLangExecutionEngine();
                e.Run(output);
            }
            Console.Read();
        }        
    }
}
