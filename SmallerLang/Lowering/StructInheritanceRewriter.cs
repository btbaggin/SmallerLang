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
        Dictionary<string, StructSyntax> _structsToPoly;
        Dictionary<string, StructSyntax> _polydStructs;
        protected override SyntaxNode VisitModuleSyntax(ModuleSyntax pNode)
        {
            _module = pNode;
            _structsToPoly = new Dictionary<string, StructSyntax>();
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
                
                if (s.TypeParameters.Count > 0) _structsToPoly.Add(n.Name, n);
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
                        break;
                    }
                }
                fields.AddRange(pNode.Fields);

                return SyntaxFactory.Struct(pNode.Name, pNode.Inherits, pNode.Methods, fields, pNode.TypeParameters);
            }

            return base.VisitStructSyntax(pNode);
        }

        protected override SyntaxNode VisitStructInitializerSyntax(StructInitializerSyntax pNode)
        {
            if(pNode.Struct.GenericArguments.Count > 0 && _structsToPoly.ContainsKey(pNode.Struct.Value))
            {
                var s = _structsToPoly[pNode.Struct.Value];
                if (TryPolyStruct(s, ref pNode))
                {
                    return pNode;
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

            Dictionary<string, int> typeArgMapping = new Dictionary<string, int>();

            //Get struct name
            var structName = new StringBuilder(pNode.Name);
            structName.Append("<");
            for(int i = 0; i < types.Count; i++)
            {
                structName.Append(types[i].Value + ",");
                typeArgMapping.Add(pNode.TypeParameters[i], i);
            }
            structName = structName.Remove(structName.Length - 1, 1);
            structName.Append(">");

            if (!_polydStructs.ContainsKey(structName.ToString()))
            {
                //Rebuild a new struct with the generic types replaced
                List<TypedIdentifierSyntax> fields = new List<TypedIdentifierSyntax>(pNode.Fields.Count);
                List<MethodSyntax> methods = new List<MethodSyntax>(pNode.Methods.Count);
                foreach (var f in pNode.Fields)
                {
                    if (!typeArgMapping.ContainsKey(f.TypeNode.Value)) fields.Add(f);
                    else
                    {
                        var i = typeArgMapping[f.TypeNode.Value];
                        fields.Add(SyntaxFactory.TypedIdentifier(types[i], f.Value));
                    }
                }

                foreach(var m in pNode.Methods)
                {
                    List<TypedIdentifierSyntax> parameters = new List<TypedIdentifierSyntax>();
                    List<TypeSyntax> returnValues = new List<TypeSyntax>();
                    //Poly method parameters
                    foreach(var p in m.Parameters)
                    {
                        if (!typeArgMapping.ContainsKey(p.TypeNode.Value)) parameters.Add(p);
                        else
                        {
                            var i = typeArgMapping[p.TypeNode.Value];
                            parameters.Add(SyntaxFactory.TypedIdentifier(types[i], p.Value));
                        }
                    }

                    //Poly method return values
                    foreach(var r in m.ReturnValues)
                    {
                        if (!typeArgMapping.ContainsKey(r.Value)) returnValues.Add(r);
                        else
                        {
                            var i = typeArgMapping[r.Value];
                            returnValues.Add(types[i]);
                        }
                    }

                    var mNew = (MethodSyntax)SyntaxFactory.Method(m.Name, returnValues, parameters, (BlockSyntax)Visit(m.Body)).FromNode(m);
                    methods.Add(mNew);
                }

                _polydStructs.Add(structName.ToString(), SyntaxFactory.Struct(structName.ToString(), pNode.Inherits, methods, fields, new List<string>()));
            }

            List<ExpressionSyntax> arguments = new List<ExpressionSyntax>(pInitializer.Arguments.Count);
            foreach(var a in pInitializer.Arguments)
            {
                arguments.Add(Visit((dynamic)a));
            }

            pInitializer = SyntaxFactory.StructInitializer(pInitializer.Value, SyntaxFactory.Type(structName.ToString()), arguments);

            return true;
        }
    }
}
