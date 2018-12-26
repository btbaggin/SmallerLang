using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;
using SmallerLang.Utils;

namespace SmallerLang.Lowering
{
    partial class TreeRewriter : SyntaxNodeRewriter
    {
        readonly Dictionary<string, List<TypeDefinitionSyntax>> _implements = new Dictionary<string, List<TypeDefinitionSyntax>>();
        Dictionary<string, Dictionary<string, GenericTypeSyntax>> _types = new Dictionary<string, Dictionary<string, GenericTypeSyntax>>();
        string _currentType;

        protected override SyntaxNode VisitModuleSyntax(ModuleSyntax pNode)
        {
            //Build our list for discovering types
            for (int i = 0; i < pNode.Structs.Count; i++)
            {
                var s = pNode.Structs[i];
                if (s.DefinitionType == DefinitionTypes.Implement)
                {
                    var applies = SyntaxHelper.GetFullTypeName(s.AppliesTo);
                    if (!_implements.ContainsKey(applies)) _implements.Add(applies, new List<TypeDefinitionSyntax>());
                    _implements[applies].Add(s);
                }
            }

            return base.VisitModuleSyntax(pNode);
        }

        protected override SyntaxNode VisitTypeDefinitionSyntax(TypeDefinitionSyntax pNode)
        {
            //Merge trait fields into this struct
            _currentType = SyntaxHelper.GetFullTypeName(pNode.GetApplicableType());

            //Create our generic parameter types
            if(!_types.ContainsKey(_currentType))
            {
                _types.Add(_currentType, new Dictionary<string, GenericTypeSyntax>());
                foreach (var t in pNode.TypeParameters)
                {
                    var T = SmallTypeCache.CreateGenericParameter(t);
                    var arrT = SmallTypeCache.CreateGenericParameter(SmallTypeCache.GetArrayType(T), T);

                    _types[_currentType].Add(T.Name, SyntaxFactory.GenericType(T));
                    _types[_currentType].Add(arrT.Name,  SyntaxFactory.GenericType(arrT));
                }
            }

            //Update any field identifier types to use the generic parm
            List<TypedIdentifierSyntax> fields = new List<TypedIdentifierSyntax>(pNode.Fields.Count); 
            for (int i = 0; i < pNode.Fields.Count; i++)
            {
                var type = pNode.Fields[i].TypeNode.Value;

                var ti = pNode.Fields[i];
                if(_types[_currentType].ContainsKey(type))
                {
                    ti = SyntaxFactory.TypedIdentifier(_types[_currentType][type], pNode.Fields[i].Value);
                }

                fields.Add(ti);
            }

            //Update any trait field identifier types to use the generic parm
            if (_implements.ContainsKey(_currentType) && pNode.DefinitionType != DefinitionTypes.Implement)
            {
                foreach (var trait in _implements[_currentType])
                {
                    foreach (var field in trait.Fields)
                    {
                        var type = field.TypeNode.Value;
                        var ti = field;
                        if (_types[_currentType].ContainsKey(type))
                        {
                            ti = SyntaxFactory.TypedIdentifier(_types[_currentType][type], field.Value);
                        }

                        fields.Add(ti);
                    }
                }
            }

            List<MethodSyntax> methods = new List<MethodSyntax>(pNode.Methods.Count);
            foreach (var m in pNode.Methods)
            {
                methods.Add((MethodSyntax)Visit(m));
            }

            //Poly the type it applies to use the generic type
            var appliesTo = pNode.AppliesTo;
            if(appliesTo != null)
            {
                List<TypeSyntax> genericArgs = new List<TypeSyntax>();
                foreach(var ga in appliesTo.GenericArguments)
                {
                    if(_types[_currentType].ContainsKey(ga.Value))
                    {
                        genericArgs.Add(_types[_currentType][ga.Value]);
                    }
                    else
                    {
                        genericArgs.Add(ga);
                    }
                }

                appliesTo = SyntaxFactory.Type(appliesTo.Namespace, appliesTo.Value, genericArgs);
            }

            _currentType = null;
            return SyntaxFactory.TypeDefinition(pNode.DeclaredType, appliesTo, pNode.DefinitionType, methods, fields);
        }

        protected override SyntaxNode VisitMethodSyntax(MethodSyntax pNode)
        {
            if(_currentType != null && _types[_currentType].Count > 0)
            {
                //Poly any parameters
                List<TypedIdentifierSyntax> parameters = new List<TypedIdentifierSyntax>(pNode.Parameters.Count);
                foreach (var p in pNode.Parameters)
                {
                    if (_types[_currentType].ContainsKey(p.TypeNode.Value))
                    {
                        parameters.Add(SyntaxFactory.TypedIdentifier(_types[_currentType][p.TypeNode.Value], p.Value));
                    }
                    else
                    {
                        parameters.Add(p);
                    }
                }

                //Poly return types
                List<TypeSyntax> returnValues = new List<TypeSyntax>(pNode.ReturnValues.Count);
                foreach (var r in pNode.ReturnValues)
                {
                    if (_types[_currentType].ContainsKey(r.Value))
                    {
                        returnValues.Add(_types[_currentType][r.Value]);
                    }
                    else
                    {
                        returnValues.Add(r);
                    }
                }

                return SyntaxFactory.Method(pNode.Name, returnValues, parameters, (BlockSyntax)Visit(pNode.Body));
            }

            return base.VisitMethodSyntax(pNode);
        }

        protected override SyntaxNode VisitArrayLiteralSyntax(ArrayLiteralSyntax pNode)
        {
            if (_currentType != null && _types[_currentType].ContainsKey(pNode.TypeNode.Value))
            {
                return SyntaxFactory.ArrayLiteral(_types[_currentType][pNode.TypeNode.Value], pNode.Size);
            }
            return base.VisitArrayLiteralSyntax(pNode);
        }
    }
}
