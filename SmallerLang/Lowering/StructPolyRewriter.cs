using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;
using SmallerLang.Emitting;

namespace SmallerLang.Lowering
{
    class PolyRewriter : Validation.SyntaxNodeVisitor
    {
        readonly Dictionary<string, TypeDefinitionSyntax> _structsToPoly;
        readonly Dictionary<string, List<TypeDefinitionSyntax>> _implements;
        readonly IErrorReporter _error;

        public PolyRewriter(IErrorReporter pError)
        {
            _error = pError;
            _structsToPoly = new Dictionary<string, TypeDefinitionSyntax>();
            _implements = new Dictionary<string, List<TypeDefinitionSyntax>>();
        }

        protected override void VisitModuleSyntax(ModuleSyntax pNode)
        {
            //Add and mark structs for poly
            foreach (var s in pNode.Structs)
            {
                if (s.TypeParameters.Count > 0) _structsToPoly.Add(s.Name, s);
                if (s.DefinitionType == DefinitionTypes.Implement)
                {
                    var applies = TypeSyntax.GetFullTypeName(s.AppliesTo);
                    if (!_implements.ContainsKey(applies)) _implements.Add(applies, new List<TypeDefinitionSyntax>());
                    _implements[applies].Add(s);
                }
            }
            base.VisitModuleSyntax(pNode);
        }

        protected override void VisitStructInitializerSyntax(StructInitializerSyntax pNode)
        {
            if(pNode.Struct.GenericArguments.Count > 0 && _structsToPoly.ContainsKey(pNode.Struct.Value))
            {
                var s = _structsToPoly[pNode.Struct.Value];
                TryPolyStruct(s, pNode);
            }
            base.VisitStructInitializerSyntax(pNode);
        }

        private void TryPolyStruct(TypeDefinitionSyntax pNode, StructInitializerSyntax pInitializer)
        {
            if(pInitializer.Struct.GenericArguments.Count != pNode.TypeParameters.Count)
            {
                _error.WriteError("Incorrect number of type arguments supplied to struct, expecting " + pNode.TypeParameters.Count, pInitializer.Span);
            }

            Dictionary<string, SmallType> types = new Dictionary<string, SmallType>();
            for (int i = 0; i < pNode.TypeParameters.Count; i++)
            {
                types.Add(pNode.TypeParameters[i], pInitializer.Struct.GenericArguments[i].Type);
            }
            pNode.AddTypeMapping(types);

            //We also need to add the same type mappings for any trait implementations
            var name = TypeSyntax.GetFullTypeName(pNode.GetApplicableType());
            if (_implements.ContainsKey(name))
            {
                foreach(var impl in _implements[name])
                {
                    impl.AddTypeMapping(types);
                }
            }
        }
    }
}
