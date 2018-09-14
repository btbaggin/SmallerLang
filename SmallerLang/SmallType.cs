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

        public bool IsStruct { get; internal set; }

        public bool IsTrait { get; internal set; }

        public bool IsEnum { get; internal set; }

        public bool IsTuple { get; internal set; }

        public string Name { get; private set; }

        private readonly SmallType _elementType;
        private readonly FieldDefinition[] _fields;

        private readonly List<SmallType> _implements;

        //Value constructor
        internal SmallType(string pName)
        {
            Name = pName;
        }

        //Struct constructor
        internal SmallType(string pName, FieldDefinition[] pFields)
        {
            Name = pName;
            _fields = pFields;
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
            System.Diagnostics.Debug.Assert(pFields.Length == pValues.Length);

            FieldDefinition[] fields = new FieldDefinition[pFields.Length];
            for (int i = 0; i < fields.Length; i++)
            {
                fields[i] = new FieldDefinition(this, pFields[i], pValues[i]);
            }
            Name = pName;
            _fields = fields;
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
                if (_fields[i].Name == pField) return _fields[i].Type;
            }
            return SmallTypeCache.Undefined;
        }

        public SmallType GetFieldType(int pIndex)
        {
            if (pIndex >= _fields.Length) return SmallTypeCache.Undefined;
            return _fields[pIndex].Type;
        }

        public int GetFieldIndex(string pField)
        {
            if (_fields == null) return -1;
            for (int i = 0; i < _fields.Length; i++)
            {
                if (_fields[i].Name == pField) return i;
            }
            return -1;
        }

        public int GetEnumValue(string pEnum)
        {
            var i = GetFieldIndex(pEnum);
            if(i > -1) return (int)_fields[i].Value;
            return -1;
        }

        public IEnumerable<FieldDefinition> GetFields()
        {
            if(_fields != null)
            {
                for (int i = 0; i < _fields.Length; i++)
                {
                    yield return _fields[i];
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
