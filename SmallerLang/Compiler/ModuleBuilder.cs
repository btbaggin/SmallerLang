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
            Parallel.ForEach(pModule.Imports, (i) =>
            {
                var alias = i.Key;
                var node = i.Value;
                var mod = BuildInternal(node, alias);

                if (mod != null) main.Cache.AddReference(alias, mod);
            });

            if (!main.Compile(main.Cache)) return null;
            
            return main;
        }
    }
}
