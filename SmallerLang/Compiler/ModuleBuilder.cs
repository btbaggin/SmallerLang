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
            var module = BuildInternal(pWorkspace.Module, null);
            return module;
        }

        private static CompilationModule BuildInternal(ModuleSyntax pModule, string pNamespace)
        {
            CompilationModule main = new CompilationModule(pModule, pNamespace);
            foreach (var i in pModule.Imports)
            {
                var alias = i.Key;
                var node = i.Value;
                //if (node.Imports.Count > 0)
                //{
                    var mod = BuildInternal(node, alias);
                    main.Cache.AddReference(alias, mod);
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

            main.Compile(main.Cache);
            
            return main;
        }
    }
}
