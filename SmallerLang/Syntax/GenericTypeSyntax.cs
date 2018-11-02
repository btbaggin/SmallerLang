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

        //TODO I don't like this constructor
        internal GenericTypeSyntax(string pValue, SmallType pElementType) : base(null, pValue, new List<TypeSyntax>())
        {
            _type = SmallTypeCache.CreateGenericParameter(pValue, pElementType);
        }
    }
}
