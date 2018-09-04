using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang;

namespace SmallerLangRun
{ 
    static class Program
    {
        /*TODO
         * Do struct poly after type inference?
         * Set multiple variables from single assignment?
         * imports
         * yield statement?
         * strings
         * Generics
        */
        static void Main(string[] args)
        {
            //switch(args[0])
            //{
            //    case "compile":
            //        var prog = args[1];
            //        var output = args[2];
            //        var c = new SmallCompiler();
            //        var o = new CompilerOptions(output) {
            //            OutputBytecode = true,
            //            Optimizations = false,
            //            SourceFile = prog
            //        };
            //        c.Compile(o);
            //        break;

            //    case "run":
            //        if(System.IO.File.Exists(args[1]))
            //        {
            //            var e = new SmallLangExecutionEngine();
            //            e.Run(args[1]);
            //        }
            //        break;
            //}
            var prog = @"C:\Test\SML\Poly.sml";
            var output = @"C:\Test\SML\test.bc";
            var c = new SmallCompiler();
            var o = new CompilerOptions(output)
            {
                OutputBytecode = false,
                Optimizations = true,
                SourceFile = prog
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
