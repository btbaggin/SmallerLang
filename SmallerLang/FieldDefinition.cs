using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmallerLang
{
    public enum FieldVisibility
    {
        Hidden,
        Public
    }

    public struct FieldDefinition
    {
        public SmallType Type { get; private set; }
        public string Name { get; private set; }
        public object Value { get; private set; }
        public FieldVisibility Visibility { get; private set; }

        public FieldDefinition(SmallType pType, string pName) : this(pType, pName, FieldVisibility.Public) { }

        public FieldDefinition(SmallType pType, string pName, FieldVisibility pVisibility)
        {
            Type = pType;
            Name = pName;
            Value = null;
            Visibility = pVisibility;
        }

        public FieldDefinition(SmallType pType, string pName, object pValue) : this(pType, pName, pValue, FieldVisibility.Public) { }

        public FieldDefinition(SmallType pType, string pName, object pValue, FieldVisibility pVisibility)
        {
            Type = pType;
            Name = pName;
            Value = pValue;
            Visibility = pVisibility;
        }
    }
}
