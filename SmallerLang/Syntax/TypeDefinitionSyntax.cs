﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using SmallerLang.Emitting;
using SmallerLang.Utils;

namespace SmallerLang.Syntax
{
    public enum DefinitionTypes
    {
        Unknown,
        Struct,
        Trait,
        Implement
    }

    public class TypeDefinitionSyntax : SyntaxNode
    {
        public override SmallType Type => SmallTypeCache.Undefined;

        public override SyntaxType SyntaxType => SyntaxType.TypeDefinition;

        public DefinitionTypes DefinitionType { get; private set; }

        public TypeSyntax DeclaredType { get; private set; }

        public TypeSyntax AppliesTo { get; private set; }

        public List<TypedIdentifierSyntax> Fields { get; private set; }

        public List<MethodSyntax> Methods { get; private set; }

        public List<string> TypeParameters { get; private set; }

        internal int EmitOrder { get; set; }

        readonly private List<Dictionary<string, SmallType>> _typeMappings;

        public string Name
        {
            get { return DeclaredType.Value; }
        }

        public FileScope Scope { get; private set; }

        internal TypeDefinitionSyntax(FileScope pScope,
                                      TypeSyntax pType, 
                                      TypeSyntax pImplements,
                                      DefinitionTypes pDefinitionType,
                                      List<TypedIdentifierSyntax> pFields, 
                                      List<MethodSyntax> pMethods)
        {
            System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(pType.Value), "Define name cannot be empty");
            System.Diagnostics.Debug.Assert(pDefinitionType != DefinitionTypes.Unknown);

            Scope = pScope;
            DeclaredType = pType;
            DefinitionType = pDefinitionType;
            AppliesTo = pImplements;
            Fields = pFields;
            Methods = pMethods;

            var tp = new List<string>();
            foreach(var pa in DeclaredType.GenericArguments)
            {
                tp.Add(pa.Value);
            }
            TypeParameters = tp;
            _typeMappings = new List<Dictionary<string, SmallType>>();
        }

        #region Methods
        public void EmitMethodHeaders(EmittingContext pContext)
        {
            //Only structs should be emitted. The other types get merged with structs
            if (DefinitionType == DefinitionTypes.Trait) return;

            Emit((pType) =>
            {
                foreach (var m in Methods)
                {
                    m.EmitHeader(pContext);
                }

                if (!pType.HasDefinedConstructor() && DefinitionType == DefinitionTypes.Struct)
                    EmitGenericConstructorHeader(pContext, pType);
            }, pContext);
        }

        public void EmitMethods(EmittingContext pContext)
        {
            //Only structs should be emitted. The other types get merged with structs
            if (DefinitionType == DefinitionTypes.Trait) return;

            Emit((pType) =>
            {
                foreach (var m in Methods)
                {
                    m.Emit(pContext);
                }

                if (!pType.HasDefinedConstructor() && DefinitionType == DefinitionTypes.Struct)
                    EmitGenericConstructor(pContext, pType);
            }, pContext);
        }
        #endregion

        private void Emit(Action<SmallType> pAction, EmittingContext pContext)
        {
            var applicableType = GetApplicableType();
            //If the type is generic, but we didn't get any poly members for it, ignore it
            //This means there were no references to it so it's ok to not emit it
            //This is kinda hacky but it works
            if (applicableType.Type.HasGenericArguments && _typeMappings.Count == 0) return;

            var result = pContext.Cache.FromString(applicableType, out SmallType type);
            System.Diagnostics.Debug.Assert(result == Compiler.FindResult.Found);

            if(_typeMappings.Count > 0)
            {
                //If this is a generic type we need to go through each of the concrete definitions and emit everything
                //Ie would emit two copies of everything
                //T -> int, U -> bool
                //T -> float, U -> float 
                foreach (var t in _typeMappings)
                {
                    //The type we want to pass around is the concrete definition of this generic type
                    var concreteType = pContext.Cache.MakeConcreteType(type, t.Values.ToArray());

                    pContext.CurrentStruct = concreteType;
                    pContext.TypeMappings = t;
                    pAction.Invoke(concreteType);
                    pContext.CurrentStruct = null;
                    pContext.TypeMappings = null;
                }
            }
            else
            {
                pContext.CurrentStruct = type;
                pAction.Invoke(type);
                pContext.CurrentStruct = null;
            }
        }

        #region Types
        public override LLVMValueRef Emit(EmittingContext pContext)
        {
            //We need both structs and traits to have definitions because either could be referenced as a type
            if(DefinitionType != DefinitionTypes.Implement)
            {
                Emit((pType) =>
                {
                    pContext.EmitDefinition(this);
                }, pContext);
            }

            return default;
        }

        private void EmitGenericConstructorHeader(EmittingContext pContext, SmallType pType)
        {
            //Emit method header
            var ret = LLVMTypeRef.VoidType();

            Emit((pT) =>
            {
                var parm = new LLVMTypeRef[] { LLVMTypeRef.PointerType(SmallTypeCache.GetLLVMType(pType, pContext), 0) };

                pContext.EmitMethodHeader(TypeHelper.GetDefaultConstructorName(pType), ret, parm);
            }, pContext);
        }

        private void EmitGenericConstructor(EmittingContext pContext, SmallType pType)
        {
            var func = pContext.GetMethod(TypeHelper.GetDefaultConstructorName(pType));
            pContext.StartMethod(func);

            var body = LLVM.AppendBasicBlock(func, Name + "body");
            LLVM.PositionBuilderAtEnd(pContext.Builder, body);

            LLVMValueRef parm = LLVM.GetParam(func, 0);

            //Emit field assignments
            var fields = pType.GetFields();
            for (int i = 0; i < fields.Length; i++)
            {
                //Set all fields to their default value
                LLVMValueRef value = SmallTypeCache.GetLLVMDefault(fields[i].Type, pContext);

                var indexAccess = LLVM.BuildInBoundsGEP(pContext.Builder, parm, new LLVMValueRef[] { pContext.GetInt(0), pContext.GetInt(i) }, "field_" + fields[i].Value);
                LLVM.BuildStore(pContext.Builder, value, indexAccess);
            }

            LLVM.BuildRetVoid(pContext.Builder);
        }
        #endregion

        public override T FromNode<T>(T pNode)
        {
            EmitOrder = (pNode as TypeDefinitionSyntax).EmitOrder;
            return base.FromNode(pNode);
        }

        public TypeSyntax GetApplicableType()
        {
            return DefinitionType != DefinitionTypes.Implement ? DeclaredType : AppliesTo;
        }

        internal void AddTypeMapping(Dictionary<string, SmallType> pTypes)
        {
            //Only add a new type mapping if we don't have one already
            foreach (var tm in _typeMappings)
            {
                if (pTypes.SequenceEqual(tm)) return;
            }

            _typeMappings.Add(pTypes);
        }

        public override string ToString()
        {
            return DefinitionType.ToString() + " " + DeclaredType.Value;
        }
    }
}
