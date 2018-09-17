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

        public DefinitionTypes DefinitionType { get; private set; }

        public string Name { get; private set; }

        public string AppliesTo { get; private set; }

        public IList<TypedIdentifierSyntax> Fields { get; private set; }

        public IList<MethodSyntax> Methods { get; private set; }

        public IList<string> TypeParameters { get; private set; }

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
            if (DefinitionType == DefinitionTypes.Struct)
            {
                var type = SmallTypeCache.FromString(Name);
                pContext.CurrentStruct = type;
                foreach (var m in Methods)
                {
                    m.EmitHeader(pContext);
                }

                if (!type.HasDefinedConstructor()) EmitGenericConstructorHeader(pContext);
                pContext.CurrentStruct = null;
            }
        }

        public void EmitMethods(EmittingContext pContext)
        {
            //Only structs should be emitted. The other types get merged with structs
            if(DefinitionType == DefinitionTypes.Struct)
            {
                var type = SmallTypeCache.FromString(Name);
                pContext.CurrentStruct = type;
                foreach (var m in Methods)
                {
                    m.Emit(pContext);
                }

                if (!type.HasDefinedConstructor()) EmitGenericConstructor(pContext, type);
                pContext.CurrentStruct = null;
            }
        }

        public override LLVMValueRef Emit(EmittingContext pContext)
        {
            if(DefinitionType == DefinitionTypes.Struct)
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

            var b = LLVM.AppendBasicBlock(func, Name + "body");
            LLVM.PositionBuilderAtEnd(pContext.Builder, b);

            LLVMValueRef p = LLVM.GetParam(func, 0);

            //Emit field assignments
            for (int i = 0; i < Fields.Count; i++)
            {
                var t = pType.GetField(Fields[i].Value).Type;
                LLVMValueRef value = SmallTypeCache.GetLLVMDefault(t, pContext);

                int f = pType.GetFieldIndex(Fields[i].Value);
                var indexAccess = LLVM.BuildInBoundsGEP(pContext.Builder, p, new LLVMValueRef[] { pContext.GetInt(0), pContext.GetInt(f) }, "field_" + Fields[i].Value);

                LLVM.BuildStore(pContext.Builder, value, indexAccess);
            }

            LLVM.BuildRetVoid(pContext.Builder);
        }
    }
}
