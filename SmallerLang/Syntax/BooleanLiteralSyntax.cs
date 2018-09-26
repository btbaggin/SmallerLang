using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using SmallerLang.Emitting;

namespace SmallerLang.Syntax
{
    public class BooleanLiteralSyntax : IdentifierSyntax
    {
        public override SmallType Type => SmallTypeCache.Boolean;

        internal BooleanLiteralSyntax(string pValue) : base(pValue) { }

        public override LLVMValueRef Emit(EmittingContext pContext)
        {
            pContext.EmitDebugLocation(this);

            ulong v = bool.Parse(Value) ? 1ul : 0ul;
            return LLVM.ConstInt(LLVM.Int1Type(), v, EmittingContext.False);
        }
    }
}
