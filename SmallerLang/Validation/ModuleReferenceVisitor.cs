﻿using System;
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
        public Compiler.CompilationCache Unit { get; private set; }

        public ReferencedNode(SyntaxNode pNode, Compiler.CompilationCache pUnit)
        {
            Node = pNode;
            Unit = pUnit;
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

            if (_module != null || Namespace != null)
            {
                System.Diagnostics.Debug.Assert(_module != null || _unit.HasReference(Namespace));

                var mod = Namespace == null ? _module : _unit.GetReference(Namespace);
                foreach (var m in mod.Module.Methods)
                {
                    if (IsCalledMethod(m, pNode)) //TODO && !Nodes.Contains(m))
                    {
                        MethodNodes.Add(new ReferencedNode(m, mod.Cache));

                        //Get any type/methods that this method references
                        var mrv = new ModuleReferenceVisitor(mod.Cache, _context, mod);
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

            if (_module != null || !string.IsNullOrEmpty(pNode.Type.Namespace))
            {
                System.Diagnostics.Debug.Assert(_module != null || _unit.HasReference(pNode.Type.Namespace));

                var mod = Namespace == null ? _module : _unit.GetReference(pNode.Type.Namespace);
                foreach(var t in mod.Module.Structs)
                {
                    bool add = false;
                    if (pNode.Type == t.DeclaredType.Type)//TODO && !UsedTypes.Contains(t))
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
                        TypeNodes.Add(new ReferencedNode(t, mod.Cache));

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
