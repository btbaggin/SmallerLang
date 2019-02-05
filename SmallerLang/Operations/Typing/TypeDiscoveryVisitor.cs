using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;
using SmallerLang.Emitting;
using SmallerLang.Utils;

namespace SmallerLang.Operations.Typing
{
    class TypeDiscoveryVisitor : SyntaxNodeVisitor 
    {
        readonly Dictionary<string, List<TypeDefinitionSyntax>> _implements;
        readonly TypeDiscoveryGraph _discoveryGraph;
        readonly Compiler.CompilationCache _unit;
        int _order;

        public TypeDiscoveryVisitor(Compiler.CompilationCache pUnit)
        {
            _discoveryGraph = new TypeDiscoveryGraph();
            _implements = new Dictionary<string, List<TypeDefinitionSyntax>>();
            _unit = pUnit;
        }

        protected override void VisitModuleSyntax(ModuleSyntax pNode)
        {
            //////
            ////// Discover enums
            ////// Add enums first since they can't reference anything, but things can reference enums
            //////
            foreach (var e in pNode.Enums)
            {
                _unit.AddType(e);
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
                    _discoveryGraph.AddNode(s);
                }
            }

            /////
            ///// Discover all other types
            /////
            var nodes = _discoveryGraph.GetNodes();
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
                    //Validate that the namespace and type exist
                    var applyName = SyntaxHelper.GetFullTypeName(s.AppliesTo);
                    var name = SyntaxHelper.GetFullTypeName(s.DeclaredType);

                    //Mark any traits for types
                    if(ValidateType(s.AppliesTo.Namespace, applyName, s) && 
                       ValidateType(s.DeclaredType.Namespace, name, s))
                    {
                        var result = _unit.FromString(s.DeclaredType, out SmallType traitType);
                        System.Diagnostics.Debug.Assert(result == Compiler.FindResult.Found);

                        result = _unit.FromString(s.AppliesTo, out SmallType applyType);
                        System.Diagnostics.Debug.Assert(result == Compiler.FindResult.Found);

                        applyType.AddTrait(traitType);

                        //TODO how do I validate methods of traits that come from a module?
                        try
                        {
                            var trait = _discoveryGraph.GetNode(name).Node;
                            ValidateImplementation(trait, s);
                        }
                        catch (Exception) { }
                    }
                }
            }

            //
            // Add all methods to the MethodCache
            //
            for (int j = 0; j < pNode.Methods.Count; j++)
            {
                AddMethodToCache(null, pNode.Methods[j], out MethodDefinition m);
            }

            //
            // Add struct methods to MethodCache
            //
            foreach (var s in pNode.Structs)
            {
                var result = _unit.FromString(s.GetApplicableType(), out SmallType type);

                //This can occur if we are specifying an undeclared type
                //or a type imported from another module
                // - The namespace might not be specified
                // - The type might not be exported
                switch(result)
                {
                    case Compiler.FindResult.IncorrectScope:
                        CompilerErrors.TypeNotInScope(s.GetApplicableType().ToString(), s.Span);
                        break;

                    case Compiler.FindResult.NotFound:
                        CompilerErrors.UndeclaredType(s.GetApplicableType().ToString(), s.Span);
                        break;
                }

                //Add each method to the cache and set type constructors if necessary
                for (int j = 0; j < s.Methods.Count; j++)
                {
                    if (AddMethodToCache(type, s.Methods[j], out MethodDefinition m) && 
                        s.Methods[j].Annotation.Value == KeyAnnotations.Constructor)
                    {
                        type.SetConstructor(m);
                    }
                }

                if (!type.HasDefinedConstructor()) type.SetDefaultConstructor();
            }
        }

        private bool DiscoverTypes(string pType, TextSpan pSpan)
        {
            //If a node doesn't exist the type hasn't been declared
            if(!_discoveryGraph.NodeExists(pType))
            {
                CompilerErrors.UndeclaredType(pType, pSpan);
                return false;
            }

            //If we have been here already we have a circular reference in our types
            //ie Type1 has a field of Type2; Type2 has a field of Type1
            var item = _discoveryGraph.GetNode(pType);
            if (item.Permanent) return true;
            if (item.Temporary)
            {
                CompilerErrors.CircularReference(item.Node, item.Node.Span);
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
                   !_unit.IsTypeDefined(type.Namespace, nt) &&
                   !DiscoverTypes(nt, type.Span))
                {
                    return false;
                }
            }

            item.Permanent = true;
            
            return AddType(item.Node); 
        }

        private bool AddType(TypeDefinitionSyntax pDefinition)
        {
            //Check for duplicate type definitions
            pDefinition.EmitOrder = _order++;
            var name = SyntaxHelper.GetFullTypeName(pDefinition.DeclaredType);
            if (_unit.IsTypeDefined(name))
            {
                CompilerErrors.DuplicateType(name, pDefinition.Span);
                return true;
            }

            //Ensure types on base struct
            for (int i = 0; i < pDefinition.Fields.Count; i++)
            {
                //Ensure all types on the field have been found
                var f = pDefinition.Fields[i];
                var typeName = SyntaxHelper.GetFullTypeName(f.TypeNode);
                if (!pDefinition.TypeParameters.Contains(typeName))
                {
                    var result = _unit.FromString(f.TypeNode, out SmallType fieldType);
                    if (result != Compiler.FindResult.Found) return false;

                    f.TypeNode.SetType(fieldType);
                }
            }

            //Ensure types on any implements
            List<TypeDefinitionSyntax> implements = null;
            if (_implements.ContainsKey(pDefinition.Name))
            {
                implements = _implements[pDefinition.Name];
                foreach (var impl in implements)
                {
                    for (int i = 0; i < impl.Fields.Count; i++)
                    {
                        //Ensure all types on the field have been found
                        var f = impl.Fields[i];
                        var typeName = SyntaxHelper.GetFullTypeName(f.TypeNode);
                        if (!pDefinition.TypeParameters.Contains(typeName))
                        {
                            var result = _unit.FromString(f.TypeNode, out SmallType fieldType);
                            if (result != Compiler.FindResult.Found) return false;

                            f.TypeNode.SetType(fieldType);
                        }
                    }
                }
            }

            //Duplicate fields get checked in AddType
            //Other validate like ensuring fields/methods are present is done later after all types have been found
            //See ValidateImplementation
            _unit.AddType(pDefinition, implements);
            return true;
        }

        private bool AddMethodToCache(SmallType pType, MethodSyntax pMethod, out MethodDefinition pDefinition)
        {
            //Check for duplicate method definitions
            Compiler.FindResult found;
            if (pMethod.SyntaxType == SyntaxType.Method) found = _unit.MethodExists(pType, pMethod);
            else if (pMethod.SyntaxType == SyntaxType.CastDefinition) found = _unit.CastExists(pType, pMethod.Type, out MethodDefinition pDef);
            else throw new InvalidOperationException("Unknown method type " + pMethod.SyntaxType.ToString());

            if (found != Compiler.FindResult.NotFound)
            {
                if (pMethod.SyntaxType == SyntaxType.Method) CompilerErrors.MethodDuplicate(pMethod, pMethod.Span);
                else if (pMethod.SyntaxType == SyntaxType.CastDefinition) CompilerErrors.CastDuplicate(pMethod.Parameters[0].Type, pMethod.Type, pMethod.Span);
                pDefinition = default;
                return false;
            }
            else
            {
                //Create the tuple type if we are returning more than one value from a method
                //This will cache it in our SmallTypeCache so it can be found later
                if (pMethod.ReturnValues.Count > 1)
                {
                    SmallTypeCache.GetOrCreateTuple(SyntaxHelper.SelectNodeTypes(pMethod.ReturnValues));
                }

                //Set method and return types
                foreach (var p in pMethod.Parameters)
                {
                    _unit.FromString(p.TypeNode, out SmallType t);
                    p.TypeNode.SetType(t);
                }
                foreach (var r in pMethod.ReturnValues)
                {
                    _unit.FromString(r, out SmallType t);
                    r.SetType(t);
                }

                //Add method
                pDefinition = _unit.AddMethod(pType, pMethod);
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
                    CompilerErrors.ImplementationMissingField(tf.Value, pTrait, pImplement.Span);
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
                    CompilerErrors.ImplementationMissingMethod(tm.Name, pTrait, pImplement.Span);
                }
            }
        }

        private bool ValidateType(NamespaceSyntax pNamespace, string pType, SyntaxNode pNode)
        {
            //Validate that all namespaces exists
            //Validate that the trait type exists
            if(pNamespace != null && !_unit.HasReference(pNamespace.Value))
            {
                CompilerErrors.NamespaceNotDefined(pNamespace, pNode.Span);
                return false;
            }
            else if(!_unit.IsTypeDefined(pNamespace, pType))
            {
                CompilerErrors.UndeclaredType(pType, pNode.Span);
                return false;
            }

            return true;
        }
    }
}
