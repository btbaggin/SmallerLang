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

        public bool IsTrait { get; internal set; }

        public bool IsEnum { get; private set; }

        public bool IsTuple { get; internal set; }

        public string Name { get; private set; }

        private readonly SmallType _elementType;
        private readonly string[] _fields;
        private readonly int[] _enumValues;
        private readonly SmallType[] _fieldTypes;

        private List<SmallType> _implements;

        //Value constructor
        internal SmallType(string pName)
        {
            Name = pName;
        }

        //Struct constructor
        internal SmallType(string pName, string[] pFields, SmallType[] pFieldTypes)
        {
            System.Diagnostics.Debug.Assert(pFields.Length == pFieldTypes.Length);

            Name = pName;
            IsStruct = true;
            _fields = pFields;
            _fieldTypes = pFieldTypes;
            _implements = new List<SmallType>();
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
            _implements = new List<SmallType>() { SmallTypeCache.Int };
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

        Emitting.MethodDefinition _constructor;
        bool _constructorSet;
        public Emitting.MethodDefinition GetConstructor()
        {
            return _constructor;
        }

        internal void SetConstructor(Emitting.MethodDefinition pConstructor)
        {
            _constructor = pConstructor;
            _constructorSet = true;
        }

        internal void SetDefaultConstructor()
        {
            _constructor = new Emitting.MethodDefinition(Name + ".ctor");
        }

        internal bool HasDefinedConstructor()
        {
            return _constructorSet;
        }

        public void AddTrait(SmallType pTrait)
        {
            System.Diagnostics.Debug.Assert(pTrait.IsTrait);
            _implements.Add(pTrait);
        }

        public bool IsAssignableFrom(SmallType pType)
        {
            if (_implements == null) return this == pType;
            if (this == pType) return true;

            for(int i = 0; i < _implements.Count; i++)
            {
                if (_implements[i].IsAssignableFrom(pType)) return true;
            }
            return false;
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
