using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using SmallerLang.Emitting;

namespace SmallerLang.Syntax
{
    public class DiscardSyntax : IdentifierSyntax
    {
        public override SyntaxType SyntaxType => SyntaxType.Discard;

        internal DiscardSyntax() : base("") { }

        public override LLVMValueRef Emit(EmittingContext pContext)
        {
            throw new NotImplementedException();
        }
    }
}
