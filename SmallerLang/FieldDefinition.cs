using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmallerLang
{
    public struct FieldDefinition
    {
        public SmallType Type { get; private set; }
        public string Name { get; private set; }
        public object Value { get; private set; }

        public FieldDefinition(SmallType pType, string pName)
        {
            Type = pType;
            Name = pName;
            Value = null;
        }

        public FieldDefinition(SmallType pType, string pName, object pValue)
        {
            Type = pType;
            Name = pName;
            Value = pValue;
        }
    }
}
