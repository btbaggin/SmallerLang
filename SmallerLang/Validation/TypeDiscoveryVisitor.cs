using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;
using SmallerLang.Emitting;
using SmallerLang.Utils;

namespace SmallerLang.Validation
{
    class TypeDiscoveryVisitor : SyntaxNodeVisitor 
    {
        readonly IErrorReporter _error;
        readonly Dictionary<string, List<TypeDefinitionSyntax>> _implements;
        readonly TypeDiscoveryGraph _discoveryGraph;
        NamespaceContainer _namespace;
        int _order;


        public TypeDiscoveryVisitor(IErrorReporter pError)
        {
            _error = pError;
            _discoveryGraph = new TypeDiscoveryGraph();
            _implements = new Dictionary<string, List<TypeDefinitionSyntax>>();
        }

        protected override void VisitWorkspaceSyntax(WorkspaceSyntax pNode)
        {
            foreach(var i in pNode.Imports)
            {
                Visit(i.Value);
            }

            Visit(pNode.Module);
        }

        protected override void VisitModuleSyntax(ModuleSyntax pNode)
        {
            //////
            ////// Discover enums
            ////// Add enums first since they can't reference anything, but things can reference enums
            //////
            _namespace = NamespaceManager.GetNamespace(pNode.Namespace);

            foreach (var e in pNode.Enums)
            {
                _namespace.AddType(e);
            }

            //Build our list for discovering types
            for(int i = 0; i < pNode.Structs.Count; i++)
            {
                var s = pNode.Structs[i];
                if (s.DefinitionType == DefinitionTypes.Implement)
                {
                    var applies = SyntaxHelper.GetFullTypeName(s.AppliesTo);
                    if (!_implements.ContainsKey(applies)) _implements.Add(applies, new List<TypeDefinitionSyntax>());
                    _implements[applies].Add(s);
                }
                else
                {
                    _discoveryGraph.AddNode(pNode.Namespace, s);
                }
            }

            /////
            ///// Discover all other types
            /////
            var nodes = _discoveryGraph.GetNodes(pNode.Namespace);
            for (int i = 0; i < nodes.Count; i++)
            {
                var t = SyntaxHelper.GetFullTypeName(nodes[i].Node.DeclaredType);
                if(!nodes[i].Permanent && 
                   !nodes[i].Temporary && 
                   DiscoverTypes(t, nodes[i].Node.Span))
                {
                    //If we discover a type go back to the beginning to see if any that were dependent
                    //on this can now be typed
                    i = -1;
                }
            }

            foreach(var i in _implements)
            {
                foreach(var s in i.Value)
                {
                    var name = SyntaxHelper.GetFullTypeName(s.AppliesTo);
                    var validateTrait = ValidateType(name, s);

                    name = SyntaxHelper.GetFullTypeName(s.DeclaredType);
                    validateTrait = ValidateType(name, s) && validateTrait;


                    //Mark any traits for types
                    if(validateTrait)
                    {
                        //TODO List<undefined>???
                        var traitType = SmallTypeCache.FromString(name);
                        s.AppliesTo.Type.AddTrait(traitType);

                        var trait = _discoveryGraph.GetNode(name).Node;
                        ValidateImplementation(trait, s);
                    }
                }
            }

            //Add all methods to the MethodCache
            for (int j = 0; j < pNode.Methods.Count; j++)
            {
                AddMethodToCache(null, pNode.Methods[j], out MethodDefinition m);
            }

            //Add struct methods to MethodCache
            foreach (var s in pNode.Structs)
            {
                var t = s.GetApplicableType();

                var typeName = SyntaxHelper.GetFullTypeName(t);
                SmallType type = SmallTypeCache.FromStringInNamespace(_namespace.Alias, typeName);

                for (int j = 0; j < s.Methods.Count; j++)
                {
                    if (AddMethodToCache(type, s.Methods[j], out MethodDefinition m) && 
                        s.Methods[j].Annotation.Value == Utils.KeyAnnotations.Constructor)
                    {
                        type.SetConstructor(m);
                    }
                }

                if (!type.HasDefinedConstructor()) type.SetDefaultConstructor();
            }
        }

        private bool DiscoverTypes(string pType, TextSpan pSpan)
        {
            if(!_discoveryGraph.NodeExists(pType))
            {
                _error.WriteError($"Use of undeclared type {pType}", pSpan);
                return false;
            }

            var item = _discoveryGraph.GetNode(pType);
            if (item.Permanent) return true;
            if (item.Temporary)
            {
                _error.WriteError($"Found circular reference in struct {item.Node.Name}", item.Node.Span);
                return false;
            }

            item.Temporary = true;

            for (int i = 0; i < item.Node.Fields.Count; i++)
            {
                var type = item.Node.Fields[i].TypeNode;
                var nt = type.Value;
                if (nt.IndexOf('[') > -1) nt = nt.Substring(0, nt.IndexOf('['));

                //Only look through user defined types which should be undefined at this point
                if(!item.Node.TypeParameters.Contains(nt) && 
                   !SmallTypeCache.IsTypeDefined(nt) &&
                   !DiscoverTypes(nt, type.Span))
                {
                    return false;
                }
            }

            item.Permanent = true;
            AddType(item.Node);
            return true;
        }

        private void AddType(TypeDefinitionSyntax pDefinition)
        {
            pDefinition.EmitOrder = _order++;

            HashSet<string> fieldNames = new HashSet<string>();
            for (int i = 0; i < pDefinition.Fields.Count; i++)
            {
                var f = pDefinition.Fields[i];
                if (!fieldNames.Add(f.Value))
                {
                    _error.WriteError($"Duplicate field definition '{f.Value}' within struct {pDefinition.Name}", f.Span);
                }
            }

            _namespace.AddType(pDefinition);
        }

        private bool AddMethodToCache(SmallType pType, MethodSyntax pMethod, out MethodDefinition pDefinition)
        {
            bool found = false;
            if (pMethod.SyntaxType == SyntaxType.Method) found = _namespace.MethodExists(pType, pMethod);
            else if (pMethod.SyntaxType == SyntaxType.CastDefinition) found = MethodCache.CastExists(pType, pMethod.Type);
            else throw new InvalidOperationException("Unknown method type " + pMethod.SyntaxType.ToString());

            if (found)
            {
                if (pMethod.SyntaxType == SyntaxType.Method) _error.WriteError($"Redeclaration of method signature {pMethod}", pMethod.Span);
                else if (pMethod.SyntaxType == SyntaxType.CastDefinition) _error.WriteError($"Cast already defined for type {pMethod.Parameters[0].Type} to {pMethod.Type}");
                pDefinition = default;
                return false;
            }
            else
            {
                //Create the tuple type if we are returning more than one value from a method
                if (pMethod.ReturnValues.Count > 1)
                {
                    SmallTypeCache.GetOrCreateTuple(Utils.SyntaxHelper.SelectNodeTypes(pMethod.ReturnValues));
                }

                pDefinition = _namespace.AddMethod(pType, pMethod);
                return true;
            }
        }

        private void ValidateImplementation(TypeDefinitionSyntax pTrait, TypeDefinitionSyntax pImplement)
        {
            //Ensure that all fields are in the implementation
            foreach (var tf in pTrait.Fields)
            {
                bool found = false;
                foreach (var f in pImplement.Fields)
                {
                    if (tf.Value == f.Value)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    _error.WriteError($"Implementation of trait {pTrait.Name} is missing field {tf.Value}", pImplement.Span);
                }
            }

            //Ensure that all methods are in the implementation
            foreach (var tm in pTrait.Methods)
            {
                bool found = false;
                foreach (var im in pImplement.Methods)
                {
                    if (im.Name == tm.Name)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    _error.WriteError($"Implementation of trait {pTrait.Name} is missing method {tm.Name}", pImplement.Span);
                }
            }
        }

        private bool ValidateType(string pType, SyntaxNode pNode)
        {
            //Validate that all namespaces exists
            //Validate that the trait type exists
            var ns = SmallTypeCache.GetNamespace(ref pType);
            if (!NamespaceManager.TryGetNamespace(ns, out NamespaceContainer container))
            {
                _error.WriteError($"Namespace {ns} has not been defined", pNode.Span);
                return false;
            }
            else if (!container.IsTypeDefinedInNamespace(pType))
            {
                _error.WriteError($"Use of undeclared type {pType}", pNode.Span);
                return false;
            }

            return true;
        }
    }
}
