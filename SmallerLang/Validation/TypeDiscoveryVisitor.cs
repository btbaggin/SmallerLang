using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;
using SmallerLang.Emitting;

namespace SmallerLang.Validation
{
    class TypeDiscoveryVisitor : SyntaxNodeVisitor 
    {
        readonly IErrorReporter _error;
        Dictionary<string, int> _structIndex;
        List<(TypeDefinitionSyntax Node, bool Permanent, bool Temporary)> _structs;
        public TypeDiscoveryVisitor(IErrorReporter pError)
        {
            _error = pError;
            _structIndex = new Dictionary<string, int>();
            _structs = new List<(TypeDefinitionSyntax, bool, bool)>();
        }

        protected override void VisitModuleSyntax(ModuleSyntax pNode)
        {
            //Add enums first since they can't reference anything, but things can reference enums
            //////
            ////// Discover enums
            //////
            foreach (var e in pNode.Enums)
            {
                string[] names = new string[e.Names.Count];
                int[] values = new int[e.Names.Count];
                for (int j = 0; j < e.Names.Count; j++)
                {
                    names[j] = e.Names[j].Value;
                    values[j] = e.Values[j];
                }
                SmallTypeCache.AddEnum(e.Name, names, values);
            }

            //Build our list for discovering types
            for(int i = 0; i < pNode.Structs.Count; i++)
            {
                var s = pNode.Structs[i];
                if(s.DefinitionType != DefinitionTypes.Implement)
                {
                    _structIndex.Add(s.Name, _structs.Count);
                    _structs.Add((s, false, false));
                }
            }

            /////
            ///// Discover all other types
            /////
            for (int i = 0; i < _structs.Count; i++)
            {
                if(!_structs[i].Permanent && !_structs[i].Temporary)
                {
                    if (DiscoverTypes(_structs[i].Node.Name)) i = -1;
                }
            }

            for (int i = 0; i < pNode.Structs.Count; i++)
            {
                var s = pNode.Structs[i];
                if (s.DefinitionType == DefinitionTypes.Implement)
                {
                    //Mark any traits for types
                    var structApplies = s.AppliesTo;
                    if (!_structIndex.ContainsKey(structApplies))
                    {
                        _error.WriteError($"Use of undeclared type {structApplies}", s.Span);
                    }

                    var t = SmallTypeCache.FromString(s.Name);
                    SmallTypeCache.FromString(structApplies).AddTrait(t);

                    //Validate implementation
                    var trait = _structs[_structIndex[s.Name]].Node;
                    ValidateImplementation(trait, s);
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
                if(s.DefinitionType != DefinitionTypes.Implement)
                {
                    var type = SmallTypeCache.FromString(s.Name);
                    for (int j = 0; j < s.Methods.Count; j++)
                    {
                        if (AddMethodToCache(type, s.Methods[j], out MethodDefinition m))
                        {
                            if (s.Methods[j].Annotation == Utils.KeyAnnotations.Constructor)
                            {
                                type.SetConstructor(m);
                            }
                        }
                    }

                    if (!type.HasDefinedConstructor()) type.SetDefaultConstructor();
                }
            }
        }

        private bool DiscoverTypes(string pType)
        {
            if(!_structIndex.ContainsKey(pType))
            {
                _error.WriteError($"Use of undeclared type {pType}");
                return false;
            }

            var idx = _structIndex[pType];
            var item = _structs[idx];
            if (_structs[idx].Permanent) return true;
            if (_structs[idx].Temporary)
            {
                _error.WriteError($"Found circular reference in struct {item.Node.Name}", item.Node.Span);
                return false;
            }

            item.Temporary = true;
            _structs[idx] = item;
            for (int i = 0; i < item.Node.Fields.Count; i++)
            {
                var nt = item.Node.Fields[i].TypeNode.Value;

                //Only look through user defined types which should be undefined at this point
                if(item.Node.Fields[i].Type == SmallTypeCache.Undefined &&
                   !item.Node.TypeParameters.Contains(nt))
                {
                    if (!DiscoverTypes(nt)) return false;
                }
            }
            item.Permanent = true;
            _structs[idx] = item;
            AddType(item.Node);
            return true;
        }

        private void AddType(TypeDefinitionSyntax pDefinition)
        {
            string[] fieldNames = new string[pDefinition.Fields.Count];
            SmallType[] fieldTypes = new SmallType[pDefinition.Fields.Count];
            for (int i = 0; i < pDefinition.Fields.Count; i++)
            {
                if (fieldNames.Contains(pDefinition.Fields[i].Value))
                {
                    _error.WriteError($"Duplicate field definition: {pDefinition.Fields[i].Value} within struct {pDefinition.Name}", pDefinition.Fields[i].Span);
                }
                fieldNames[i] = pDefinition.Fields[i].Value;
                fieldTypes[i] = pDefinition.Fields[i].Type;
            }

            if (pDefinition.DefinitionType == DefinitionTypes.Struct) SmallTypeCache.AddStruct(pDefinition.Name, fieldNames, fieldTypes);
            else if (pDefinition.DefinitionType == DefinitionTypes.Trait)
            {
                SmallTypeCache.AddTrait(pDefinition.Name, fieldNames, fieldTypes);//TODO add methods
            }
        }

        private bool AddMethodToCache(SmallType pType, MethodSyntax pMethod, out MethodDefinition pDefinition)
        {
            bool found = MethodCache.MethodExists(pType, pMethod.Name, pMethod);

            if (found)
            {
                _error.WriteError($"Redeclaration of method signature {pMethod.Name}", pMethod.Span);
                pDefinition = default;
                return false;
            }
            else
            {
                //Create the tuple type if we are returning more than one value from a method
                if (pMethod.ReturnValues.Count > 1)
                {
                    SmallTypeCache.GetTempTuple(Utils.SyntaxHelper.SelectNodeTypes(pMethod.ReturnValues));
                }
                pDefinition = MethodCache.AddMethod(pType, pMethod.Name, pMethod);
                return true;
            }
        }

        private void ValidateImplementation(TypeDefinitionSyntax pTrait, TypeDefinitionSyntax pImplement)
        {
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
    }
}
