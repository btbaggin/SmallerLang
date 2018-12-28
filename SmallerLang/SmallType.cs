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

        public string Namespace { get; private set; }

        public bool IsGenericParameter { get; internal set; }

        public bool IsGenericType { get; internal set; }

        public SmallType[] GenericArguments { get; internal set; }

        public IList<string> GenericParameters { get; internal set; }

        public bool HasGenericArguments
        {
            get { return GenericArguments.Length > 0; }
        }

        public IList<SmallType> Implements => _implements ?? new List<SmallType>(0);

        private readonly SmallType _elementType;
        private readonly FieldDefinition[] _fields;

        private readonly List<SmallType> _implements;

        #region Constructors
        //Value constructor
        internal SmallType(string pName)
        {
            Name = pName;
        }

        //Struct constructor
        internal SmallType(string pNamespace, string pName, FieldDefinition[] pFields)
        {
            Namespace = pNamespace;
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
        internal SmallType(string pNamespace, string pName, string[] pFields, int[] pValues)
        {
            System.Diagnostics.Debug.Assert(pFields.Length == pValues.Length);

            FieldDefinition[] fields = new FieldDefinition[pFields.Length];
            for (int i = 0; i < fields.Length; i++)
            {
                fields[i] = new FieldDefinition(this, pFields[i], pValues[i]);
            }

            Namespace = pNamespace;
            Name = pName;
            _fields = fields;
            _implements = new List<SmallType>() { SmallTypeCache.Int };
        }
        #endregion

        public SmallType MakeArrayType()
        {
            return SmallTypeCache.FromString(SmallTypeCache.GetArrayType(this));
        }

        public SmallType GetElementType()
        {
            if (_elementType == null) return SmallTypeCache.Undefined;
            return _elementType;
        }

        public FieldDefinition GetField(string pField)
        {
            if (_fields != null)
            {
                for (int i = 0; i < _fields.Length; i++)
                {
                    if (_fields[i].Name == pField) return _fields[i];
                }
            }
            return new FieldDefinition(SmallTypeCache.Undefined, null);
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

        public FieldDefinition[] GetFields()
        {
            if(_fields != null) return _fields;
            return new FieldDefinition[] { };
        }

        public int GetFieldCount()
        {
            return _fields.Length;
        }

        #region Type constructor methods
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

        internal void SetDefaultConstructor(List<SmallType> pArguments)
        {
            _constructor = new Emitting.MethodDefinition(Name + ".ctor", pArguments);
        }

        internal bool HasDefinedConstructor()
        {
            return _constructorSet;
        }
        #endregion

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
