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
        readonly Dictionary<string, int> _structIndex;
        readonly Dictionary<string, List<TypeDefinitionSyntax>> _implements;
        int _order;

        readonly List<(TypeDefinitionSyntax Node, bool Permanent, bool Temporary)> _structs;

        public TypeDiscoveryVisitor(IErrorReporter pError)
        {
            _error = pError;
            _structIndex = new Dictionary<string, int>();
            _structs = new List<(TypeDefinitionSyntax, bool, bool)>();
            _implements = new Dictionary<string, List<TypeDefinitionSyntax>>();
        }

        protected override void VisitModuleSyntax(ModuleSyntax pNode)
        {
            //////
            ////// Discover enums
            //////
            //Add enums first since they can't reference anything, but things can reference enums
            foreach (var e in pNode.Enums)
            {
                string[] fields = new string[e.Names.Count];
                int[] values = new int[e.Names.Count];
                for (int j = 0; j < fields.Length; j++)
                {
                    fields[j] = e.Names[j].Value;
                    values[j] = e.Values[j];
            }
                SmallTypeCache.AddEnum(e.Name, fields, values);
            }

            //Build our list for discovering types
            for(int i = 0; i < pNode.Structs.Count; i++)
            {
                var s = pNode.Structs[i];
                if (s.DefinitionType == DefinitionTypes.Implement)
                {
                    if (!_implements.ContainsKey(s.AppliesTo)) _implements.Add(s.AppliesTo, new List<TypeDefinitionSyntax>());
                    _implements[s.AppliesTo].Add(s);
                }
                else
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
                if(!_structs[i].Permanent && 
                   !_structs[i].Temporary && 
                   DiscoverTypes(_structs[i].Node.Name))
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
                    //Mark any traits for types
                    bool validateTrait = true;
                    if (!SmallTypeCache.IsTypeDefined(s.Name))
                    {
                        _error.WriteError($"Use of undeclared type {s.Name}", s.Span);
                        validateTrait = false;
                    }

                    if (!SmallTypeCache.IsTypeDefined(s.AppliesTo))
                    {
                        _error.WriteError($"Use of undeclared type {s.AppliesTo}", s.Span);
                        validateTrait = false;
                    }
                    
                    if(validateTrait)
                    {
                        var traitType = SmallTypeCache.FromString(s.Name);
                        SmallTypeCache.FromString(s.AppliesTo).AddTrait(traitType);

                        //Validate implementation
                        var trait = _structs[_structIndex[s.Name]].Node;
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
                SmallType type = s.GetApplicableType();

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
                if (nt.IndexOf('[') > -1) nt = nt.Substring(0, nt.IndexOf('['));

                //Only look through user defined types which should be undefined at this point
                if(!item.Node.TypeParameters.Contains(nt) && 
                   !SmallTypeCache.IsTypeDefined(nt) &&
                   !DiscoverTypes(nt))
                {
                    return false;
                }
            }

            item.Permanent = true;
            _structs[idx] = item;
            AddType(item.Node);
            return true;
        }

        private void AddType(TypeDefinitionSyntax pDefinition)
        {
            pDefinition.EmitOrder = _order++;

            HashSet<string> fieldNames = new HashSet<string>();
            List<FieldDefinition> fields = new List<FieldDefinition>();
            for (int i = 0; i < pDefinition.Fields.Count; i++)
            {
                var f = pDefinition.Fields[i];
                if (!fieldNames.Add(f.Value))
                {
                    _error.WriteError($"Duplicate field definition: {f.Value} within struct {pDefinition.Name}", f.Span);
                }
                else
                {
                    FieldVisibility visibility = f.Annotation.Value == Utils.KeyAnnotations.Hidden ? FieldVisibility.Hidden : FieldVisibility.Public;
                    fields.Add(new FieldDefinition(pDefinition.Fields[i].Type, pDefinition.Fields[i].Value, visibility));
                }
            }
             
            //Merge trait fields into this struct
            if(_implements.ContainsKey(pDefinition.Name))
            {
                foreach(var trait in _implements[pDefinition.Name])
                {
                    foreach (var f in trait.Fields)
                    {
                        if(fieldNames.Add(f.Value))
                        {
                            FieldVisibility visibility = f.Annotation.Value == Utils.KeyAnnotations.Hidden ? FieldVisibility.Hidden : FieldVisibility.Public;
                            fields.Add(new FieldDefinition(f.Type, f.Value, visibility));
                        }
                    }
                }
            }

            if (pDefinition.DefinitionType == DefinitionTypes.Struct) SmallTypeCache.AddStruct(pDefinition.Name, fields.ToArray());
            else if (pDefinition.DefinitionType == DefinitionTypes.Trait) SmallTypeCache.AddTrait(pDefinition.Name, fields.ToArray());
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
                    SmallTypeCache.GetOrCreateTuple(Utils.SyntaxHelper.SelectNodeTypes(pMethod.ReturnValues));
                }

                pDefinition = MethodCache.AddMethod(pType, pMethod.Name, pMethod);
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
    }
}
