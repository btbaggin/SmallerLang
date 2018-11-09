using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmallerLang.Syntax
{
    public class GenericTypeSyntax : TypeSyntax
    {
        readonly SmallType _type;
        public override SmallType Type
        {
            get { return _type; }
        }

        internal GenericTypeSyntax(SmallType pType) : base(null, pType.Name, new List<TypeSyntax>())
        {
            _type = pType;
        }
    }
}
