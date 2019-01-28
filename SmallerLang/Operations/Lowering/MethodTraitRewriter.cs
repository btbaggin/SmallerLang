using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;
using SmallerLang.Emitting;

namespace SmallerLang.Operations.Lowering
{
    partial class PostTypeRewriter : SyntaxNodeRewriter
    {
        /*
         *This class will rewrite all methods with one or more trait parameter
         *It will inspect all calling sites of the method
         * and create copies of the method with the trait parameters replaced with their concrete types
         */

        readonly Dictionary<string, List<MethodSyntax>> _methodsToPoly;
        readonly Dictionary<string, List<MethodSyntax>> _polydMethods;
        Compiler.CompilationCache _unit;

        public PostTypeRewriter(Compiler.CompilationCache pUnit)
        {
            _methodsToPoly = new Dictionary<string, List<MethodSyntax>>();
            _polydMethods = new Dictionary<string, List<MethodSyntax>>();
            _unit = pUnit;
            GetEnumerable();
        }

        protected override SyntaxNode VisitModuleSyntax(ModuleSyntax pNode)
        {
            _locals.AddScope();
            FindConstants(pNode);

            //Find all methods we need to polymorph
            //A method needs to be polymorphed if any of it's parameters are traits
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
            //Visit any non-poly nodes
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

            _locals.RemoveScope();

            return SyntaxFactory.Module(pNode.Imports, methods, pNode.Structs, pNode.Enums, pNode.Fields);
        }

        protected override SyntaxNode VisitMethodCallSyntax(MethodCallSyntax pNode)
        {
            if(_methodsToPoly.ContainsKey(pNode.Value))
            {
                var method = _unit.MatchMethod(pNode, _methodsToPoly[pNode.Value]);
                if (method == null) throw new InvalidOperationException("Unable to find matching method");

                if(TryPolyMethod(method, ref pNode))
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
                    TypedIdentifierSyntax parm;
                    if (pMethod.Parameters[i].Type.IsTrait)
                    {
                        //Ensure the argument implements the proper trait... we haven't done type checking yet
                        if (!pCallSite.Arguments[i].Type.IsAssignableFrom(pMethod.Parameters[i].Type))
                        {
                            return false;
                        }

                        parm = SyntaxFactory.TypedIdentifier(SyntaxFactory.Type(pCallSite.Arguments[i].Type.Name), pMethod.Parameters[i].Value);
                    }
                    else
                    {
                        parm = (TypedIdentifierSyntax)Visit(pMethod.Parameters[i]);
                    }
                    parameters.Add(parm);
                }

                var method = SyntaxFactory.Method(pMethod.Scope, name.ToString(), pMethod.ReturnValues, parameters, (BlockSyntax)Visit(pMethod.Body)).FromNode(pMethod);
                var tiv = new Typing.TypeInferenceVisitor(_unit);
                tiv.Visit(method);
                _unit.AddMethod(null, method);

                if (!_polydMethods.ContainsKey(name.ToString())) _polydMethods.Add(name.ToString(), new List<MethodSyntax>());
                _polydMethods[name.ToString()].Add(method);
            }

            //Have the call site point to the new method
            List<SyntaxNode> arguments = new List<SyntaxNode>(pCallSite.Arguments.Count);
            foreach (var a in pCallSite.Arguments)
            {
                arguments.Add(Visit(a));
            }

            pCallSite = SyntaxFactory.MethodCall(name.ToString(), arguments);

            return true;
        }
    }
}
