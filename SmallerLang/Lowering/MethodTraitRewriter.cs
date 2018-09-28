﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;
using SmallerLang.Emitting;

namespace SmallerLang.Lowering
{
    class MethodTraitRewriter : SyntaxNodeRewriter
    {
        readonly Dictionary<string, List<MethodSyntax>> _methodsToPoly;
        readonly Dictionary<string, List<MethodSyntax>> _polydMethods;
        readonly IErrorReporter _error;

        public MethodTraitRewriter(IErrorReporter pError)
        {
            _error = pError;
            _methodsToPoly = new Dictionary<string, List<MethodSyntax>>();
            _polydMethods = new Dictionary<string, List<MethodSyntax>>();
        }

        protected override SyntaxNode VisitModuleSyntax(ModuleSyntax pNode)
        {
            //Find all methods we need to polymorph
            foreach (var m in pNode.Methods)
            {
                foreach(var p in m.Parameters)
                {
                    if(p.Type.IsTrait)
                    {
                        if (!_methodsToPoly.ContainsKey(m.Name)) _methodsToPoly.Add(m.Name, new List<MethodSyntax>());
                        _methodsToPoly[m.Name].Add(m);
                        break;
                    }
                }
            }

            List<MethodSyntax> methods = new List<MethodSyntax>(pNode.Methods.Count);
            foreach (var m in pNode.Methods)
            {
                if(!_methodsToPoly.ContainsKey(m.Name))
                {
                    methods.Add((MethodSyntax)Visit(m));
                }       
            }
            foreach(var v in _polydMethods.Values)
            {
                methods.AddRange(v);
            }

            return SyntaxFactory.Module(pNode.Name, methods, pNode.Structs, pNode.Enums);
        }

        protected override SyntaxNode VisitMethodCallSyntax(MethodCallSyntax pNode)
        {
            if(_methodsToPoly.ContainsKey(pNode.Value))
            {
                var m = MethodCache.MatchMethod(pNode, _methodsToPoly[pNode.Value]);
                if (m == null) throw new InvalidOperationException("Unable to find matching method");

                if(TryPolyMethod(m, ref pNode))
                {
                    return pNode;
                }
            }
            return base.VisitMethodCallSyntax(pNode);
        }

        private bool TryPolyMethod(MethodSyntax pMethod, ref MethodCallSyntax pCallSite)
        {
            System.Diagnostics.Debug.Assert(pMethod.Parameters.Count == pCallSite.Arguments.Count);

            //Get name of the new method
            StringBuilder name = new StringBuilder(pMethod.Name + "!!!");
            for (int i = 0; i < pMethod.Parameters.Count; i++)
            {
                if(pMethod.Parameters[i].Type.IsTrait)
                {
                    name.Append(pCallSite.Arguments[i].Type.Name + "_");
                }
            }
            name = name.Remove(name.Length - 1, 1);

            //Ensure we haven't polymorphed this method before
            if (!_polydMethods.ContainsKey(name.ToString()))
            {
                List<TypedIdentifierSyntax> parameters = new List<TypedIdentifierSyntax>(pMethod.Parameters.Count);
                for (int i = 0; i < pMethod.Parameters.Count; i++)
                {
                    if (pMethod.Parameters[i].Type.IsTrait)
                    {
                        //Ensure the argument implements the proper trait... we haven't done type checking yet
                        if (!pCallSite.Arguments[i].Type.IsAssignableFrom(pMethod.Parameters[i].Type))
                        {
                            return false;
                        }

                        var ti = SyntaxFactory.TypedIdentifier(SyntaxFactory.Type(pCallSite.Arguments[i].Type.Name), pMethod.Parameters[i].Value);
                        parameters.Add(ti);
                    }
                    else
                    {
                        parameters.Add((TypedIdentifierSyntax)Visit(pMethod.Parameters[i]));
                    }
                }

                //TODO poly me!
                List<TypeSyntax> returnValues = new List<TypeSyntax>(pMethod.ReturnValues.Count);
                foreach(var r in pMethod.ReturnValues)
                {
                    returnValues.Add((TypeSyntax)Visit(r));
                }

                var method = SyntaxFactory.Method(name.ToString(), returnValues, parameters, (BlockSyntax)Visit(pMethod.Body)).FromNode(pMethod);

                //Infer types for all nodes in the new method
                var tiv = new Validation.TypeInferenceVisitor(_error);
                tiv.Visit(method);
                MethodCache.AddMethod(name.ToString(), method);

                if (!_polydMethods.ContainsKey(name.ToString())) _polydMethods.Add(name.ToString(), new List<MethodSyntax>());
                _polydMethods[name.ToString()].Add(method);
            }

            //Have the call site point to the new method
            List<SyntaxNode> arguments = new List<SyntaxNode>(pCallSite.Arguments.Count);
            foreach (var a in pCallSite.Arguments)
            {
                arguments.Add(Visit((dynamic)a));
            }

            pCallSite = SyntaxFactory.MethodCall(name.ToString(), arguments);

            return true;
        }
    }
}
