using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;
using SmallerLang.Emitting;

namespace SmallerLang.Validation
{
    struct ReferencedNode
    {
        public SyntaxNode Node { get; private set; }
        public Compiler.CompilationUnit Unit { get; private set; }

        public ReferencedNode(SyntaxNode pNode, Compiler.CompilationUnit pUnit)
        {
            Node = pNode;
            Unit = pUnit;
        }
    }

    class ModuleReferenceVisitor : SyntaxNodeVisitor
    {
        readonly EmittingContext _context;
        readonly Compiler.CompilationModule _module;
        readonly Compiler.CompilationUnit _unit;
        public List<ReferencedNode> MethodNodes { get; private set; }
        public List<ReferencedNode> TypeNodes { get; private set; }

        public ModuleReferenceVisitor(Compiler.CompilationUnit pUnit, EmittingContext pContext, Compiler.CompilationModule pModule = null)
        {
            _context = pContext;
            _module = pModule;
            _unit = pUnit;
            MethodNodes = new List<ReferencedNode>();
            TypeNodes = new List<ReferencedNode>();
        }

        protected override void VisitMethodCallSyntax(MethodCallSyntax pNode)
        {
            base.VisitMethodCallSyntax(pNode);

            if (_module != null || Namespace != null)
            {
                System.Diagnostics.Debug.Assert(_module != null || _unit.HasReference(Namespace));

                var mod = Namespace == null ? _module : _unit.GetReference(Namespace);
                foreach (var m in mod.Module.Methods)
                {
                    if (IsCalledMethod(m, pNode)) //TODO && !Nodes.Contains(m))
                    {
                        MethodNodes.Add(new ReferencedNode(m, mod.Unit));

                        //Get any type/methods that this method references
                        var mrv = new ModuleReferenceVisitor(mod.Unit, _context, mod);
                        mrv.Visit(m);
                        MethodNodes.AddRange(mrv.MethodNodes);
                        TypeNodes.AddRange(mrv.TypeNodes);
                    }
                }
            }
        }

        protected override void VisitTypeSyntax(TypeSyntax pNode)
        {
            base.VisitTypeSyntax(pNode);

            //TODO needs work
            var name = pNode.Type.Name;
            var ns = SmallTypeCache.GetNamespace(ref name);
            if (_module != null || !string.IsNullOrEmpty(ns))
            {
                System.Diagnostics.Debug.Assert(_module != null || _unit.HasReference(ns));

                var mod = Namespace == null ? _module : _unit.GetReference(ns);
                foreach(var t in mod.Module.Structs)
                {
                    if (pNode.Type == t.Type)//TODO && !UsedTypes.Contains(t))
                    {
                        TypeNodes.Add(new ReferencedNode(t, mod.Unit));

                        //Get any type/methods that this method references
                        foreach(var m in t.Methods)
                        {
                            var mrv = new ModuleReferenceVisitor(mod.Unit, _context, mod);
                            mrv.Visit(m);
                            MethodNodes.AddRange(mrv.MethodNodes);
                            TypeNodes.AddRange(mrv.TypeNodes);
                        }
                    }
                }
            }
        }

        private bool IsCalledMethod(MethodSyntax pMethod, MethodCallSyntax pCall)
        {
            if (pMethod.Name != pCall.Value) return false;
            if (pMethod.Parameters.Count != pCall.Arguments.Count) return false;
            for(int i = 0; i < pMethod.Parameters.Count; i++)
            {
                if (!pMethod.Parameters[i].Type.IsAssignableFrom(pCall.Arguments[i].Type)) return false;
            }
            return true;
        }
    }
}
