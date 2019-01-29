using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;
using SmallerLang.Utils;

namespace SmallerLang.Operations.Lowering
{
    /*
     * This class will ensure that all generic parameter types are propagated throughout the type
     * struct Test<T>
     *   T: Field
     *   
     * This visitor will rewrite all instances of "T" to refer to the same type
     */
    partial class PreTypeRewriter : SyntaxNodeRewriter
    {
        readonly Dictionary<string, List<TypeDefinitionSyntax>> _implements = new Dictionary<string, List<TypeDefinitionSyntax>>();
        Dictionary<string, GenericTypeSyntax> _types = new Dictionary<string, GenericTypeSyntax>();
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
            if(pNode.TypeParameters.Count == 0)
            {
                return base.VisitTypeDefinitionSyntax(pNode);
            }

             _currentType = SyntaxHelper.GetFullTypeName(pNode.GetApplicableType());

            //Create our generic parameter types
            _types.Clear();
            foreach (var t in pNode.TypeParameters)
            {
                var T = SmallTypeCache.CreateGenericParameter(t);
                var arrT = SmallTypeCache.CreateGenericParameter(SmallTypeCache.GetArrayType(T), T);

                _types.Add(T.Name, SyntaxFactory.GenericType(T));
                _types.Add(arrT.Name,  SyntaxFactory.GenericType(arrT));
            }

            //Update any field identifier types to use the generic parameter type
            List<TypedIdentifierSyntax> fields = new List<TypedIdentifierSyntax>(pNode.Fields.Count); 
            for (int i = 0; i < pNode.Fields.Count; i++)
            {
                var type = PolyType(pNode.Fields[i].TypeNode);
                var iden = pNode.Fields[i].Value;

                fields.Add(SyntaxFactory.TypedIdentifier(type, iden));
            }

            //Update any trait field identifier types to use the generic parameter types
            if (_implements.ContainsKey(_currentType))
            {
                foreach (var trait in _implements[_currentType])
                {
                    foreach (var field in trait.Fields)
                    {
                        var type = PolyType(field.TypeNode);
                        var iden = field.Value;

                        fields.Add(SyntaxFactory.TypedIdentifier(type, iden));
                    }
                }
            }

            //Ensure all methods are using the new generic parameter types
            List<MethodSyntax> methods = new List<MethodSyntax>(pNode.Methods.Count);
            foreach (var m in pNode.Methods)
            {
                methods.Add((MethodSyntax)Visit(m));
            }

            //Poly the type it applies to use the generic type
            var appliesTo = pNode.AppliesTo;
            if(appliesTo != null) appliesTo = PolyType(pNode.AppliesTo);

            _currentType = null;
            return SyntaxFactory.TypeDefinition(pNode.Scope, pNode.DeclaredType, appliesTo, pNode.DefinitionType, methods, fields);
        }

        protected override SyntaxNode VisitMethodSyntax(MethodSyntax pNode)
        {
            if(_currentType != null)
            {
                //This means we are current in a generic struct and we should use the types defined on the generic struct
                //Poly any parameters
                List<TypedIdentifierSyntax> parameters = new List<TypedIdentifierSyntax>(pNode.Parameters.Count);
                foreach (var p in pNode.Parameters)
                {
                    var type = PolyType(p.TypeNode);
                    var iden = p.Value;

                    parameters.Add(SyntaxFactory.TypedIdentifier(type, iden));
                }

                //Poly return types
                List<TypeSyntax> returnValues = new List<TypeSyntax>(pNode.ReturnValues.Count);
                foreach (var r in pNode.ReturnValues)
                {
                    returnValues.Add(PolyType(r));
                }

                return SyntaxFactory.Method(pNode.Scope, pNode.Name, returnValues, parameters, (BlockSyntax)Visit(pNode.Body));
            }
            else
            {
                //Otherwise we are just in a normal generic method
                //We need to discover all the generic types then poly them
            }

            return base.VisitMethodSyntax(pNode);
        }

        protected override SyntaxNode VisitArrayLiteralSyntax(ArrayLiteralSyntax pNode)
        {
            if (_types.ContainsKey(pNode.TypeNode.Value))
            {
                return SyntaxFactory.ArrayLiteral(PolyType(pNode.TypeNode), pNode.Size);
            }
            return base.VisitArrayLiteralSyntax(pNode);
        }

        private TypeSyntax PolyType(TypeSyntax pNode)
        {
            List<TypeSyntax> genericArgs = new List<TypeSyntax>(pNode.GenericArguments.Count);
            foreach(var a in pNode.GenericArguments)
            {
                genericArgs.Add(PolyType(a));
            }

            if (_types.ContainsKey(pNode.Value))
            {
                return _types[pNode.Value];
            }

            return SyntaxFactory.Type(pNode.Namespace, pNode.Value, genericArgs);
        }
    }
}
