using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using SmallerLang.Emitting;

namespace SmallerLang.Syntax
{
    public class StructSyntax : SyntaxNode
    {
        public string Name { get; private set; }

        public string Inherits { get; private set; }

        public IList<MethodSyntax> Methods { get; private set; }

        public IList<TypedIdentifierSyntax> Fields { get; private set; }

        public IList<ExpressionSyntax> Defaults { get; private set; }

        public IList<string> TypeParameters { get; private set; }

        public override SmallType Type => SmallTypeCache.Undefined;

        internal StructSyntax(string pName, string pInherits, IList<MethodSyntax> pMethods, IList<TypedIdentifierSyntax> pFields, IList<ExpressionSyntax> pDefaults, IList<string> pTypeParms)
        {
            System.Diagnostics.Debug.Assert(pFields.Count == pDefaults.Count, "Field and default count do not match");
            System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(pName), "Define name cannot be empty");

            Name = pName;
            Inherits = pInherits;
            Methods = pMethods;
            Fields = pFields;
            Defaults = pDefaults;
            TypeParameters = pTypeParms;
        }

        public void EmitMethods(EmittingContext pContext)
        {
            var type = SmallTypeCache.FromString(Name);
            pContext.CurrentStruct = type;
            foreach(var m in Methods)
            {
                m.EmitHeader(pContext);
            }

            foreach (var m in Methods)
            {
                m.Emit(pContext);
            }

            if (!type.HasDefinedConstructor()) EmitGenericConstructor(pContext, type);
            pContext.CurrentStruct = null;
        }

        public override LLVMValueRef Emit(EmittingContext pContext)
        {
            pContext.EmitDefinition(Name, this);

            return default;
        }

        private void EmitGenericConstructor(EmittingContext pContext, SmallType pType)
        {
            //Emit method header
            var ret = LLVMTypeRef.VoidType();
            var parm = new LLVMTypeRef[] { LLVMTypeRef.PointerType(SmallTypeCache.GetLLVMType(pType), 0) };
            var func = pContext.EmitMethodHeader(Name + ".ctor", ret, parm);

            var b = LLVM.AppendBasicBlock(func, Name + "body");
            LLVM.PositionBuilderAtEnd(pContext.Builder, b);

            LLVMValueRef p = LLVM.GetParam(func, 0);

            //Emit field assignments
            for (int i = 0; i < Defaults.Count; i++)
            {
                LLVMValueRef value = Defaults[i] != null ? Defaults[i].Emit(pContext) : SmallTypeCache.GetLLVMDefault(pType.GetFieldType(Fields[i].Value), pContext);

                int f = pType.GetFieldIndex(Fields[i].Value);
                var a = LLVM.BuildInBoundsGEP(pContext.Builder, p, new LLVMValueRef[] { pContext.GetInt(0), pContext.GetInt(f) }, "field_" + Fields[i].Value);

                LLVM.BuildStore(pContext.Builder, value, a);
            }

            LLVM.BuildRetVoid(pContext.Builder);
        }
    }
}
