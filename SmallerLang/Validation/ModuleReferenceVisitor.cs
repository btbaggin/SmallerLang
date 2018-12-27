using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;
using SmallerLang.Emitting;

namespace SmallerLang.Validation
{
    class ModuleReferenceVisitor : SyntaxNodeVisitor
    {
        readonly string _namespace;
        readonly ModuleSyntax _module;
        public List<MethodSyntax> UsedMethods { get; private set; }
        public List<TypeDefinitionSyntax> UsedTypes { get; private set; }

        public ModuleReferenceVisitor(string pNamespace, ModuleSyntax pModule)
        {
            _namespace = pNamespace;
            _module = pModule;
            UsedMethods = new List<MethodSyntax>();
            UsedTypes = new List<TypeDefinitionSyntax>();
        }

        protected override void VisitMethodCallSyntax(MethodCallSyntax pNode)
        {
            if (Namespace != null && _namespace == Namespace)
            {
                foreach(var m in _module.Methods)
                {
                    if (!UsedMethods.Contains(m))
                    {
                        UsedMethods.Add(m);

                        //Get any type/methods that this method references
                        Visit(m);
                    }
                }
            }
        }

        protected override void VisitTypeSyntax(TypeSyntax pNode)
        {
            if (Namespace != null && _namespace == Namespace)
            {
                foreach(var t in _module.Structs)
                {
                    if (!UsedTypes.Contains(t))
                    {
                        UsedTypes.Add(t);

                        //Get any type/methods that this method references
                        foreach(var m in t.Methods)
                        {
                            Visit(m);
                        }
                    }
                }
            }
        }
    }
}
