using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmallerLang.Syntax
{
    public class SelfSyntax : IdentifierSyntax
    {
        internal SelfSyntax() : base("self") { }
    }
}
