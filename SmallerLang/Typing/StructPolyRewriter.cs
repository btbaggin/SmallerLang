using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;
using SmallerLang.Utils;

namespace SmallerLang.Lowering
{
    class PolyRewriter : Validation.SyntaxNodeVisitor
    {
        /*
         * This class will rewrite all structs with any generic type parameters
         * It will inspect all initializations of this type
         * and create copies of the type with the type parameters replaced with their concrete types
         */
        readonly Dictionary<string, TypeDefinitionSyntax> _structsToPoly;
        readonly Dictionary<string, List<TypeDefinitionSyntax>> _implements;

        public PolyRewriter()
        {
            _structsToPoly = new Dictionary<string, TypeDefinitionSyntax>();
            _implements = new Dictionary<string, List<TypeDefinitionSyntax>>();
        }

        protected override void VisitModuleSyntax(ModuleSyntax pNode)
        {
            //Add and mark structs for poly
            foreach (var s in pNode.Structs)
            {
                //Find all implementations and generic structs
                if (s.DefinitionType == DefinitionTypes.Implement)
                {
                    var applies = SyntaxHelper.GetFullTypeName(s.AppliesTo);
                    if (!_implements.ContainsKey(applies)) _implements.Add(applies, new List<TypeDefinitionSyntax>());
                    _implements[applies].Add(s);
                }
                else if (s.TypeParameters.Count > 0) _structsToPoly.Add(s.Name, s);
            }
            base.VisitModuleSyntax(pNode);
        }

        protected override void VisitStructInitializerSyntax(StructInitializerSyntax pNode)
        {
            //This is a initialization of a generic struct, try to poly the definition to match this call site
            if(pNode.Struct.GenericArguments.Count > 0 && _structsToPoly.ContainsKey(pNode.Struct.Value))
            {
                var s = _structsToPoly[pNode.Struct.Value];
                TryPolyStruct(s, pNode);
            }
            base.VisitStructInitializerSyntax(pNode);
        }

        private void TryPolyStruct(TypeDefinitionSyntax pNode, StructInitializerSyntax pInitializer)
        {
            //Number of generic type parameters on the type does not match 
            //the number of type arguments specified at the initialization site
            if(pInitializer.Struct.GenericArguments.Count != pNode.TypeParameters.Count)
            {
                CompilerErrors.TypePolyArgumentCount(pInitializer.Struct, pNode.TypeParameters.Count, pInitializer.Span);
            }

            //We just create a dictionary of T,int; T2,string; etc... and feed that to the type
            //When the type is emitted it will create a copy of itself for each unique generic argument
            Dictionary<string, SmallType> types = new Dictionary<string, SmallType>();
            for (int i = 0; i < pNode.TypeParameters.Count; i++)
            {
                types.Add(pNode.TypeParameters[i], pInitializer.Struct.GenericArguments[i].Type);
            }
            //TODO does this handle unique types at different call sites
            pNode.AddTypeMapping(types);

            //We also need to add the same type mappings for any trait implementations
            var name = SyntaxHelper.GetFullTypeName(pNode.GetApplicableType());
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
