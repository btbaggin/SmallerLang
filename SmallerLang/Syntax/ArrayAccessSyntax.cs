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

        public ExpressionSyntax Index { get; private set; }

        public ArrayAccessSyntax(string pVariable, ExpressionSyntax pIndex) : base(pVariable)
        {
            Index = pIndex;
        }

        public override LLVMValueRef Emit(EmittingContext pContext)
        {
            System.Diagnostics.Debug.Assert(pContext.Locals.IsVariableDefined(Value));
            var i = Index.Emit(pContext);
            var v = pContext.Locals.GetVariable(Value, out bool p);

            Utils.LlvmHelper.LoadIfPointer(ref i, pContext);

            LLVMValueRef g = LLVM.BuildInBoundsGEP(pContext.Builder, v, new LLVMValueRef[] { pContext.GetInt(0), pContext.GetInt(1) }, "arrayaccess");
            var l = LLVM.BuildLoad(pContext.Builder, g, "");
            return LLVM.BuildInBoundsGEP(pContext.Builder, l, new LLVMValueRef[] { i }, "arrayaccess");
        }
    }
}
