using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;

namespace SmallerLang.Compiler
{
    static class ModuleBuilder
    {
        public static CompilationModule Build(WorkspaceSyntax pWorkspace)
        {
            var module = BuildInternal(pWorkspace.Module);
            return module;
        }

        private static CompilationModule BuildInternal(ModuleSyntax pModule)
        {
            CompilationModule main = new CompilationModule(pModule);
            foreach (var i in pModule.Imports)
            {
                var alias = i.Key;
                var node = i.Value;
                //if (node.Imports.Count > 0)
                //{
                    var mod = BuildInternal(node);
                    main.Unit.AddReference(alias, mod);
                //}
            }

            //foreach (var i in pModule.Imports)
            //{
            //    var alias = i.Key;
            //    var node = i.Value;
            //    if(node.Imports.Count == 0)
            //    {
            //        var mod = BuildInternal(node);
            //        main.Unit.AddReference(alias, mod);
            //    }
            //}

            main.Compile(main.Unit);
            
            return main;
        }
    }
}
