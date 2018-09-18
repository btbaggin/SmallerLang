using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmallerLang.Syntax
{
    public struct Annotation
    {
        public string Value { get; private set; }
        public TextSpan Span { get; private set; }

        public Annotation(string pValue, TextSpan pSpan)
        {
            Value = pValue;
            Span = pSpan;
        }
    }
}
