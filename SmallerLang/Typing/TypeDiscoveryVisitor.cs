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
                        SmallType traitType = _unit.FromString(s.DeclaredType.Namespace, name);
                        SmallType applyType = _unit.FromString(s.AppliesTo.Namespace, applyName);

                        applyType.AddTrait(traitType);

                        var trait = _discoveryGraph.GetNode(applyName).Node;
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
                SmallType type = _unit.FromString(typeName);

                for (int j = 0; j < s.Methods.Count; j++)
                {
                    if (AddMethodToCache(type, s.Methods[j], out MethodDefinition m) && 
                        s.Methods[j].Annotation.Value == KeyAnnotations.Constructor)
                    {
                        type.SetConstructor(m);
                    }
                }

                if (!type.HasDefinedConstructor()) type.SetDefaultConstructor(new List<SmallType>());
            }
        }

        private bool DiscoverTypes(string pType, TextSpan pSpan)
        {
            if(!_discoveryGraph.NodeExists(pType))
            {
                CompilerErrors.UndeclaredType(pType, pSpan);
                return false;
            }

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
                   !SmallTypeCache.IsTypeDefined(nt) &&
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
            pDefinition.EmitOrder = _order++;
            var name = SyntaxHelper.GetFullTypeName(pDefinition.DeclaredType);
            if (_unit.IsTypeDefined(name))
            {
                CompilerErrors.DuplicateType(name, pDefinition.Span);
                return true;
            }

            HashSet<string> fieldNames = new HashSet<string>();
            for (int i = 0; i < pDefinition.Fields.Count; i++)
            {
                var f = pDefinition.Fields[i];
                var typeName = SyntaxHelper.GetFullTypeName(f.TypeNode);
                if (!pDefinition.TypeParameters.Contains(typeName))
                {
                    var fieldType = _unit.FromString(typeName);
                    if (fieldType == SmallTypeCache.Undefined) return false;

                    f.TypeNode.SetType(fieldType);
                }

                if (!fieldNames.Add(f.Value))
                {
                    CompilerErrors.DuplicateField(f.Value, pDefinition, f.Span);
                }
            }

            _unit.AddType(pDefinition);
            return true;
        }

        private bool AddMethodToCache(SmallType pType, MethodSyntax pMethod, out MethodDefinition pDefinition)
        {
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
                if (pMethod.ReturnValues.Count > 1)
                {
                    SmallTypeCache.GetOrCreateTuple(SyntaxHelper.SelectNodeTypes(pMethod.ReturnValues));
                }

                foreach (var p in pMethod.Parameters)
                {
                    p.TypeNode.SetType(_unit.FromString(Utils.SyntaxHelper.GetFullTypeName(p.TypeNode)));
                }
                foreach (var r in pMethod.ReturnValues)
                {
                    r.SetType(_unit.FromString(SyntaxHelper.GetFullTypeName(r)));
                }

                pDefinition = _unit.AddMethod(pType, pMethod);
                return true;
            }
        }

        private void ValidateImplementation(TypeDefinitionSyntax pTrait, TypeDefinitionSyntax pImplement)
        {
            //Ensure that all fields are in the implementation
            foreach (var tf in pImplement.Fields)
            {
                bool found = false;
                foreach (var f in pTrait.Fields)
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
            foreach (var tm in pImplement.Methods)
            {
                bool found = false;
                foreach (var im in pTrait.Methods)
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
