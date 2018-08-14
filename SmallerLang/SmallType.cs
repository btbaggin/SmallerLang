using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmallerLang
{
    public class SmallType
    {
        public bool IsArray
        {
            get { return _elementType != null; }
        }

        public bool IsStruct { get; private set; }

        public bool IsEnum { get; private set; }

        public bool IsTuple { get; internal set; }

        public string Name { get; private set; }

        private readonly SmallType _elementType;
        private readonly SmallType _baseType;
        private readonly string[] _fields;
        private readonly int[] _enumValues;
        private readonly SmallType[] _fieldTypes;

        //Value constructor
        internal SmallType(string pName)
        {
            Name = pName;
        }

        //Struct constructor
        internal SmallType(string pName, SmallType pInherits, string[] pFields, SmallType[] pFieldTypes)
        {
            System.Diagnostics.Debug.Assert(pFields.Length == pFieldTypes.Length);

            Name = pName;
            IsStruct = true;
            _baseType = pInherits;
            _fields = pFields;
            _fieldTypes = pFieldTypes;
        }

        //Array constructor
        internal SmallType(string pName, SmallType pElementType)
        {
            Name = pName;
            _elementType = pElementType;
        }

        //Enum constructor
        internal SmallType(string pName, string[] pFields, int[] pValues)
        {
            SmallType[] fieldTypes = new SmallType[pFields.Length];
            for(int i = 0; i < fieldTypes.Length; i++)
            {
                fieldTypes[i] = this;
            }
            Name = pName;
            IsEnum = true;
            _fields = pFields;
            _enumValues = pValues;
            _fieldTypes = fieldTypes;
            _baseType = SmallTypeCache.Int;
        }

        public SmallType MakeArrayType()
        {
            return SmallTypeCache.FromString(Name + "[]");
        }

        public SmallType GetElementType()
        {
            if (_elementType == null) return SmallTypeCache.Undefined;
            return _elementType;
        }

        public SmallType GetFieldType(string pField)
        {
            if (_fields == null) return SmallTypeCache.Undefined;
            for(int i = 0; i < _fields.Length; i++)
            {
                if (_fields[i] == pField) return _fieldTypes[i];
            }
            return SmallTypeCache.Undefined;
        }

        public SmallType GetFieldType(int pIndex)
        {
            if (pIndex >= _fieldTypes.Length) return SmallTypeCache.Undefined;
            return _fieldTypes[pIndex];
        }

        public int GetFieldIndex(string pField)
        {
            if (_fields == null) return -1;
            for (int i = 0; i < _fields.Length; i++)
            {
                if (_fields[i] == pField) return i;
            }
            return -1;
        }

        public int GetEnumValue(string pEnum)
        {
            var i = GetFieldIndex(pEnum);
            if(i > -1) return _enumValues[i];
            return -1;
        }

        public IEnumerable<(string Name, SmallType Type)> GetFields()
        {
            if(_fields != null)
            {
                for (int i = 0; i < _fields.Length; i++)
                {
                    yield return (_fields[i], _fieldTypes[i]);
                }
            }
        }

        public int GetFieldCount()
        {
            return _fields.Length;
        }

        public string GetConstructor()
        {
            return IsStruct ? Name + "___new" : null;
        }

        public bool IsAssignableFrom(SmallType pType)
        {
            if (_baseType == null) return this == pType;
            return this == pType || _baseType.IsAssignableFrom(pType);
        }

        internal bool IsFloat()
        {
            return this == SmallTypeCache.Float || this == SmallTypeCache.Double;
        }

        internal bool IsInt()
        {
            return this == SmallTypeCache.Short || this == SmallTypeCache.Int || this == SmallTypeCache.Long;
        }

        internal bool IsNumber()
        {
            return this == SmallTypeCache.Float || this == SmallTypeCache.Double || this == SmallTypeCache.Int ||
                   this == SmallTypeCache.Long || this == SmallTypeCache.Short;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Name.GetHashCode() == obj.GetHashCode();
        }

        public override string ToString()
        {
            return Name;
        }    
    }
}
