using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;
using SmallerLang.Emitting;

namespace SmallerLang.Operations
{
    struct ReferencedNode : IEquatable<ReferencedNode>
    {
        public SyntaxNode Node { get; private set; }
        public Compiler.CompilationCache Unit { get; private set; }

        public ReferencedNode(SyntaxNode pNode, Compiler.CompilationCache pUnit)
        {
            Node = pNode;
            Unit = pUnit;
        }

        public bool Equals(ReferencedNode other)
        {
            switch(other.Node)
            {
                case MethodSyntax m:
                    //Check both are methods
                    var n = Node as MethodSyntax;
                    if (n == null) return false;

                    //Methods are equal if their name and paremeters are the same
                    if (n.Name != m.Name) return false;

                    if (n.Parameters.Count != m.Parameters.Count) return false;

                    for (int i = 0; i < n.Parameters.Count; i++)
                    {
                        if (n.Parameters[i].Type != m.Parameters[i].Type) return false;
                    }
                        return true;

                case TypeDefinitionSyntax t:
                    var nt = Node as TypeDefinitionSyntax;
                    if (nt == null) return false;

                    if (nt.Name != t.Name) return false;
                    if (nt.DefinitionType != t.DefinitionType) return false;
                    return true;

                default:
                    return false;
            }
        }
    }

    class ModuleReferenceVisitor : SyntaxNodeVisitor
    {
        readonly EmittingContext _context;
        readonly Compiler.CompilationModule _module;
        readonly Compiler.CompilationCache _unit;
        public List<ReferencedNode> MethodNodes { get; private set; }
        public List<ReferencedNode> TypeNodes { get; private set; }

        public ModuleReferenceVisitor(Compiler.CompilationCache pUnit, EmittingContext pContext, Compiler.CompilationModule pModule = null)
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

            //We only care about methods that aren't in the current module
            if (_module != null || Namespace != null)
            {
                System.Diagnostics.Debug.Assert(_module != null || _unit.HasReference(Namespace));

                //Get the referenced module
                var mod = Namespace == null ? _module : _unit.GetReference(Namespace);

                //Find the method
                foreach (var m in mod.Module.Methods)
                {
                    if (IsCalledMethod(m, pNode))
                    {
                        var rn = new ReferencedNode(m, mod.Cache);
                        if(!MethodNodes.Contains(rn))
                        {
                            MethodNodes.Add(rn);

                            //Get any type/methods that this method references
                            var mrv = new ModuleReferenceVisitor(mod.Cache, _context, mod);
                            mrv.MethodNodes.Add(rn);

                            mrv.Visit(m);
                            MethodNodes.AddRange(mrv.MethodNodes);
                            TypeNodes.AddRange(mrv.TypeNodes);
                        }
                    }
                }
            }
        }

        protected override void VisitCastSyntax(CastSyntax pNode)
        {
            base.VisitCastSyntax(pNode);

            //We only care about methods that aren't in the current module
            foreach(var mod in _unit.GetAllReferences())
            {
                //Find the method
                foreach (var m in mod.Module.Methods)
                {
                    if (m is CastDefinitionSyntax c && IsCalledCast(c, pNode))
                    {
                        var rn = new ReferencedNode(m, mod.Cache);
                        if (!MethodNodes.Contains(rn))
                        {
                            MethodNodes.Add(rn);

                            //Get any type/methods that this method references
                            var mrv = new ModuleReferenceVisitor(mod.Cache, _context, mod);
                            mrv.Visit(m);
                            MethodNodes.AddRange(mrv.MethodNodes);
                            TypeNodes.AddRange(mrv.TypeNodes);
                        }
                    }
                }
            }
        }

        protected override void VisitTypeSyntax(TypeSyntax pNode)
        {
            base.VisitTypeSyntax(pNode);

            if (SmallTypeCache.TryGetPrimitive(pNode.Type.Name, out SmallType tt)) return;

            //We only care about types that aren't in the current module
            if (_module != null || !string.IsNullOrEmpty(pNode.Type.Namespace))
            {
                System.Diagnostics.Debug.Assert(_module != null || _unit.HasReference(pNode.Type.Namespace));

                //Get the referenced module
                var mod = Namespace == null ? _module : _unit.GetReference(pNode.Type.Namespace);

                //Find the struct
                foreach(var t in mod.Module.Structs)
                {
                    bool add = false;
                    if (pNode.Type == t.GetApplicableType().Type)
                    {
                        add = true;
                    }
                    else if (pNode.Type != null &&
                             pNode.Type.HasGenericArguments && 
                             pNode.Type.Name == t.DeclaredType.Type.Name &&
                             pNode.Type.GenericParameters.Count == t.DeclaredType.Type.GenericParameters.Count)
                    {
                        add = true;
                    }

                    if(add)
                    {
                        var rn = new ReferencedNode(t, mod.Cache);
                        if(!TypeNodes.Contains(rn))
                        {
                            TypeNodes.Add(rn);

                            //TODO check existance before adding to typenodes?
                            //Get any type/methods that this method references
                            foreach (var m in t.Methods)
                            {
                                var mrv = new ModuleReferenceVisitor(mod.Cache, _context, mod);
                                mrv.Visit(m);
                                MethodNodes.AddRange(mrv.MethodNodes);
                                TypeNodes.AddRange(mrv.TypeNodes);
                            }
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

        private bool IsCalledCast(CastDefinitionSyntax pMethod, CastSyntax pCall)
        {
            return pMethod.Parameters[0].Type == pCall.FromType &&
                   pMethod.ReturnValues[0].Type == pCall.Type;
        }
    }
}
