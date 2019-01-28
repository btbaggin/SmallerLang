using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;
using SmallerLang.Utils;

namespace SmallerLang.Operations.Typing
{
    class PolyRewriter : SyntaxNodeVisitor
    {
        /*
         * This class will rewrite all structs with any generic type parameters
         * It will inspect all initializations of this type
         * and create copies of the type with the type parameters replaced with their concrete types
         */
        readonly Dictionary<string, TypeDefinitionSyntax> _structsToPoly;
        readonly Dictionary<string, List<TypeDefinitionSyntax>> _implements;
        readonly Compiler.CompilationCache _compilation;

        public PolyRewriter(Compiler.CompilationCache pCompilation)
        {
            _compilation = pCompilation;
            _structsToPoly = new Dictionary<string, TypeDefinitionSyntax>();
            _implements = new Dictionary<string, List<TypeDefinitionSyntax>>();
        }

        protected override void VisitModuleSyntax(ModuleSyntax pNode)
        {
            //First we go through the current module to find any types that need to poly
            foreach (var s in pNode.Structs)
            {
                MaybeQueueStructToPoly(s);
            }

            //After we have found all of those, we go through the imported modules and find additional types
            //Since we could be using those types in our main module we need to poly them
            foreach (var r in _compilation.GetAllReferences())
            {
                foreach (var s in r.Module.Structs)
                {
                    MaybeQueueStructToPoly(s);
                }
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

        private void MaybeQueueStructToPoly(TypeDefinitionSyntax pType)
        {
            //Find all implementations and generic structs
            //We will need to know this so we can poly implements of the types
            if (pType.DefinitionType == DefinitionTypes.Implement)
            {
                var applies = SyntaxHelper.GetFullTypeName(pType.AppliesTo);
                if (!_implements.ContainsKey(applies)) _implements.Add(applies, new List<TypeDefinitionSyntax>());
                _implements[applies].Add(pType);
            }
            else if (pType.TypeParameters.Count > 0)
            {
                //This is a generic type, put it in the queue to be poly'd
                _structsToPoly.Add(pType.Name, pType);
            }
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
