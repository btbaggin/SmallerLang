using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmallerLang.Syntax
{
    public class SelfSyntax : IdentifierSyntax
    {
        public override SyntaxType SyntaxType => SyntaxType.Self;

        internal SelfSyntax() : base("self") { }
    }
}
