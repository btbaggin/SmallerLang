using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using SmallerLang.Emitting;

namespace SmallerLang.Syntax
{
    public class StringLiteralSyntax : IdentifierSyntax
    {
        public override SmallType Type => SmallTypeCache.String;

        public override SyntaxType SyntaxType => SyntaxType.StringLiteral;

        internal StringLiteralSyntax(string pValue) : base(pValue) { }

        public override LLVMValueRef Emit(EmittingContext pContext)
        {
            pContext.EmitDebugLocation(this);

            return pContext.GetString(Value);
        }
    }
}
