using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using SmallerLang.Emitting;

namespace SmallerLang.Syntax
{
    public class ArrayAccessSyntax : IdentifierSyntax
    {
        public SmallType BaseType => base.Type;

        public override SmallType Type => base.Type.GetElementType();

        public IdentifierSyntax Identifier { get; private set; }

        public ExpressionSyntax Index { get; private set; }

        public ArrayAccessSyntax(IdentifierSyntax pVariable, ExpressionSyntax pIndex) : base(null)
        {
            Identifier = pVariable;
            Index = pIndex;
        }

        public override LLVMValueRef Emit(EmittingContext pContext)
        {
            pContext.EmitDebugLocation(this);

            //We are in a member access, just push the index of this field onto the stack
            LLVMValueRef variable = Identifier.Emit(pContext);
            var index = Index.Emit(pContext);

            Utils.LlvmHelper.LoadIfPointer(ref index, pContext);

            var indexAccess = LLVM.BuildInBoundsGEP(pContext.Builder, variable, new LLVMValueRef[] { pContext.GetInt(0), pContext.GetInt(1) }, "arrayaccess");
            var load = LLVM.BuildLoad(pContext.Builder, indexAccess, "");

            return LLVM.BuildInBoundsGEP(pContext.Builder, load, new LLVMValueRef[] { index }, "arrayaccess");
        }
    }
}
