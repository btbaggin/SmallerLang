using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;

namespace SmallerLang.Validation
{
    partial class TreeRewriter : SyntaxNodeRewriter
    {
        ModuleSyntax _module;
        List<StructSyntax> _structsToPoly;
        Dictionary<string, StructSyntax> _polydStructs;
        protected override SyntaxNode VisitModuleSyntax(ModuleSyntax pNode)
        {
            _module = pNode;
            _structsToPoly = new List<StructSyntax>();
            _polydStructs = new Dictionary<string, StructSyntax>();

            List<EnumSyntax> enums = new List<EnumSyntax>(pNode.Enums.Count);
            List<MethodSyntax> methods = new List<MethodSyntax>(pNode.Methods.Count);
            List<StructSyntax> structs = new List<StructSyntax>(pNode.Structs.Count);
            //Add all enumerations
            foreach (var e in pNode.Enums)
            {
                enums.Add((EnumSyntax)Visit(e));
            }

            //Add and mark structs for poly
            foreach (var s in pNode.Structs)
            {
                var n = (StructSyntax)Visit(s);
                
                if (s.TypeParameters.Count > 0) _structsToPoly.Add(n);
                else structs.Add(n);
            }


            //Visiting the methods will poly any struct AST nodes
            //It will also transform any call sites to reference the new polyd node
            foreach (var m in pNode.Methods)
            {
                methods.Add((MethodSyntax)Visit(m));
            }

            structs.AddRange(_polydStructs.Values);
            return SyntaxFactory.Module(pNode.Name, methods, structs, enums);
        }

        protected override SyntaxNode VisitStructSyntax(StructSyntax pNode)
        {
            //Add any base fields to the inherited stuct
            if (!string.IsNullOrEmpty(pNode.Inherits))
            {
                List<TypedIdentifierSyntax> fields = new List<TypedIdentifierSyntax>();
                List<ExpressionSyntax> defaults = new List<ExpressionSyntax>();
                for (int i = 0; i < _module.Structs.Count; i++)
                {
                    if (_module.Structs[i].Name == pNode.Inherits)
                    {
                        fields.AddRange(_module.Structs[i].Fields);
                        defaults.AddRange(_module.Structs[i].Defaults);
                        break;
                    }
                }
                fields.AddRange(pNode.Fields);
                defaults.AddRange(pNode.Defaults);
                return SyntaxFactory.Struct(pNode.Name, pNode.Inherits, fields, defaults, pNode.TypeParameters);
            }
            return base.VisitStructSyntax(pNode);
        }

        protected override SyntaxNode VisitStructInitializerSyntax(StructInitializerSyntax pNode)
        {
            if(pNode.Struct.GenericArguments.Count > 0)
            {
                foreach(var s in _structsToPoly)
                {
                    if(s.Name == pNode.Struct.Value && TryPolyStruct(s, ref pNode))
                    {
                        return pNode;
                    }
                }
            }
            return base.VisitStructInitializerSyntax(pNode);
        }

        private bool TryPolyStruct(StructSyntax pNode, ref StructInitializerSyntax pInitializer)
        {
            var types = pInitializer.Struct.GenericArguments;
            if(types.Count != pNode.TypeParameters.Count)
            {
                _error.WriteError("Incorrect number of type arguments supplied to struct, expecting " + pNode.TypeParameters.Count, pInitializer.Span);
                return false;
            }

            //Get struct name
            var structName = new StringBuilder(pNode.Name);
            structName.Append("(");
            foreach (var t in types)
            {
                structName.Append(t.Value + ",");
            }
            structName = structName.Remove(structName.Length - 1, 1);
            structName.Append(")");

            if (!_polydStructs.ContainsKey(structName.ToString()))
            {
                //Rebuild a new struct with the generic types replaced
                List<TypedIdentifierSyntax> fields = new List<TypedIdentifierSyntax>();
                foreach (var f in pNode.Fields)
                {
                    if (!f.TypeNode.IsGeneric) fields.Add(SyntaxFactory.TypedIdentifier(f.TypeNode, f.Value));
                    else
                    {
                        for (int i = 0; i < pNode.TypeParameters.Count; i++)
                        {
                            if (f.TypeNode.IsGeneric && f.TypeNode.Value == pNode.TypeParameters[i])
                            {
                                fields.Add(SyntaxFactory.TypedIdentifier(types[i], f.Value));
                                break;
                            }
                        }
                    }
                }

                _polydStructs.Add(structName.ToString(), SyntaxFactory.Struct(structName.ToString(), pNode.Inherits, fields, pNode.Defaults.ToList(), new List<string>()));

            }

            pInitializer = SyntaxFactory.StructInitializer(pInitializer.Value, SyntaxFactory.Type(structName.ToString()));

            return true;
        }
    }
}
