using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;

namespace SmallerLang.Lowering
{
    partial class TreeRewriter : SyntaxNodeRewriter
    {
        ModuleSyntax _module;
        Dictionary<string, TypeDefinitionSyntax> _structsToPoly;
        Dictionary<string, TypeDefinitionSyntax> _polydStructs;

        protected override SyntaxNode VisitModuleSyntax(ModuleSyntax pNode)
        {
            _module = pNode;
            _structsToPoly = new Dictionary<string, TypeDefinitionSyntax>();
            _polydStructs = new Dictionary<string, TypeDefinitionSyntax>();

            List<EnumSyntax> enums = new List<EnumSyntax>(pNode.Enums.Count);
            List<MethodSyntax> methods = new List<MethodSyntax>(pNode.Methods.Count);
            List<TypeDefinitionSyntax> structs = new List<TypeDefinitionSyntax>(pNode.Structs.Count);
            //Add all enumerations
            foreach (var e in pNode.Enums)
            {
                enums.Add((EnumSyntax)Visit(e));
            }

            //Add and mark structs for poly
            foreach (var s in pNode.Structs)
            {
                var n = (TypeDefinitionSyntax)Visit(s);
                
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

        protected override SyntaxNode VisitArrayLiteralSyntax(ArrayLiteralSyntax pNode)
        {
            if(_typeArgMapping.ContainsKey(pNode.TypeNode.Value))
            {
                var i = _typeArgMapping[pNode.TypeNode.Value];
                return SyntaxFactory.ArrayLiteral(_types[i], pNode.Value);
            }
            return base.VisitArrayLiteralSyntax(pNode);
        }

        Dictionary<string, int> _typeArgMapping = new Dictionary<string, int>();
        IList<TypeSyntax> _types;
        private bool TryPolyStruct(TypeDefinitionSyntax pNode, ref StructInitializerSyntax pInitializer)
        {
            _types = pInitializer.Struct.GenericArguments;
            if(_types.Count != pNode.TypeParameters.Count)
            {
                _error.WriteError("Incorrect number of type arguments supplied to struct, expecting " + pNode.TypeParameters.Count, pInitializer.Span);
                return false;
            }

            //Get struct name
            var structName = new StringBuilder(pNode.Name);
            structName.Append("<");
            for(int i = 0; i < _types.Count; i++)
            {
                structName.Append(_types[i].Value + ",");
                _typeArgMapping.Add(pNode.TypeParameters[i], i);
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
                    //TODO I can't poly T[] and this is awful
                    var type = f.TypeNode.Value;
                    if (f.TypeNode.Type.IsArray) type = type.Substring(0, type.IndexOf('['));

                    if (!_typeArgMapping.ContainsKey(type)) fields.Add(f);
                    else
                    {
                        var i = _typeArgMapping[type];
                        var t = SyntaxFactory.Type(_types[i].Value + (f.TypeNode.Type.IsArray ? "[]" : ""));
                        fields.Add(SyntaxFactory.TypedIdentifier(t, f.Value));
                    }
                }

                foreach (var m in pNode.Methods)
                {
                    List<TypedIdentifierSyntax> parameters = new List<TypedIdentifierSyntax>();
                    List<TypeSyntax> returnValues = new List<TypeSyntax>();
                    //Poly method parameters
                    foreach (var p in m.Parameters)
                    {
                        if (!_typeArgMapping.ContainsKey(p.TypeNode.Value)) parameters.Add(p);
                        else
                        {
                            var i = _typeArgMapping[p.TypeNode.Value];
                            parameters.Add(SyntaxFactory.TypedIdentifier(_types[i], p.Value));
                        }
                    }

                    //Poly method return values
                    foreach (var r in m.ReturnValues)
                    {
                        if (!_typeArgMapping.ContainsKey(r.Value)) returnValues.Add(r);
                        else
                        {
                            var i = _typeArgMapping[r.Value];
                            returnValues.Add(_types[i]);
                        }
                    }

                    var mNew = (MethodSyntax)SyntaxFactory.Method(m.Name, returnValues, parameters, (BlockSyntax)Visit(m.Body)).FromNode(m);
                    methods.Add(mNew);
                }

                _polydStructs.Add(structName.ToString(), SyntaxFactory.TypeDefinition(structName.ToString(), pNode.AppliesTo, pNode.DefinitionType, methods, fields, new List<string>()));
            }

            List<ExpressionSyntax> arguments = new List<ExpressionSyntax>(pInitializer.Arguments.Count);
            foreach(var a in pInitializer.Arguments)
            {
                arguments.Add(Visit((dynamic)a));
            }

            pInitializer = SyntaxFactory.StructInitializer(pInitializer.Value, SyntaxFactory.Type(structName.ToString()), arguments);

            _typeArgMapping.Clear();
            return true;
        }
    }
}
