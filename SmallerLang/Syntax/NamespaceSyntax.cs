using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using SmallerLang.Emitting;

namespace SmallerLang.Syntax
{
    public class NamespaceSyntax : IdentifierSyntax
    {
        public override SmallType Type => null;

        public override SyntaxType SyntaxType => SyntaxType.Namespace;

        internal NamespaceSyntax(string pAlias) : base(pAlias) { }

        public override LLVMValueRef Emit(EmittingContext pContext)
        {
            throw new NotImplementedException();
        }
    }
}
