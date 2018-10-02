using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using SmallerLang.Emitting;

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

        public string Name { get; private set; }

        public string AppliesTo { get; private set; }

        public IList<TypedIdentifierSyntax> Fields { get; private set; }

        public IList<MethodSyntax> Methods { get; private set; }

        public IList<string> TypeParameters { get; private set; }

        internal int EmitOrder { get; set; }

        internal TypeDefinitionSyntax(string pName, 
                                      string pImplements,
                                      DefinitionTypes pType,
                                      IList<TypedIdentifierSyntax> pFields, 
                                      IList<MethodSyntax> pMethods, 
                                      IList<string> pTypeParameters)
        {
            System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(pName), "Define name cannot be empty");
            System.Diagnostics.Debug.Assert(pType != DefinitionTypes.Unknown);

            Name = pName;
            DefinitionType = pType;
            AppliesTo = pImplements;
            Fields = pFields;
            Methods = pMethods;
            TypeParameters = pTypeParameters;
        }

        public void EmitMethodHeaders(EmittingContext pContext)
        {
            //Only structs should be emitted. The other types get merged with structs
            if (DefinitionType == DefinitionTypes.Trait) return;

            var type = GetApplicableType();
            pContext.CurrentStruct = type;
            foreach (var m in Methods)
            {
                m.EmitHeader(pContext);
            }

            if (!type.HasDefinedConstructor()) EmitGenericConstructorHeader(pContext);
            pContext.CurrentStruct = null;
        }

        public void EmitMethods(EmittingContext pContext)
        {
            //Only structs should be emitted. The other types get merged with structs
            if (DefinitionType == DefinitionTypes.Trait) return;

            var type = GetApplicableType();
            pContext.CurrentStruct = type;
            foreach (var m in Methods)
            {
                m.Emit(pContext);
            }

            if (!type.HasDefinedConstructor() && DefinitionType == DefinitionTypes.Struct) EmitGenericConstructor(pContext, type);
            pContext.CurrentStruct = null;
        }

        public override LLVMValueRef Emit(EmittingContext pContext)
        {
            //We need both structs and traits to have definitions because either could be referenced as a type
            if(DefinitionType != DefinitionTypes.Implement)
            {
                pContext.EmitDefinition(Name, this);
            }

            return default;
        }

        private void EmitGenericConstructorHeader(EmittingContext pContext)
        {
            //Emit method header
            var ret = LLVMTypeRef.VoidType();
            var parm = new LLVMTypeRef[] { LLVMTypeRef.PointerType(SmallTypeCache.GetLLVMType(SmallTypeCache.FromString(Name)), 0) };
            pContext.EmitMethodHeader(Name + ".ctor", ret, parm);
        }

        private void EmitGenericConstructor(EmittingContext pContext, SmallType pType)
        {
            var func = pContext.GetMethod(Name + ".ctor");

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

        public override T FromNode<T>(T pNode)
        {
            EmitOrder = (pNode as TypeDefinitionSyntax).EmitOrder;
            return base.FromNode(pNode);
        }

        public SmallType GetApplicableType()
        {
            return DefinitionType != DefinitionTypes.Implement ? SmallTypeCache.FromString(Name) : SmallTypeCache.FromString(AppliesTo);

        }
    }
}
