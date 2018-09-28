using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmallerLang
{
    struct VisitorValue<T> : IDisposable
    {
        readonly VisitorStore _store;
        readonly string _name;
        public T Value
        {
            get { return _store.GetValue<T>(_name); }
        }

        internal VisitorValue(VisitorStore pStore, string pName)
        {
            _store = pStore;
            _name = pName;
        }

        public void Dispose()
        {
            _store.RemoveValue(_name);
        }
    }

    class VisitorStore
    {
        readonly Dictionary<string, Stack<object>> _values;

        public VisitorStore()
        {
            _values = new Dictionary<string, Stack<object>>();
        }

        public VisitorValue<T> AddValue<T>(string pName, T pValue)
        {
            if (!_values.ContainsKey(pName)) _values.Add(pName, new Stack<object>());
            _values[pName].Push(pValue);
            return new VisitorValue<T>(this, pName);
        }

        public void SetValue<T>(string pName, T pValue)
        {
            _values[pName].Pop();
            _values[pName].Push(pValue);
        }

        public T GetValue<T>(string pName)
        {
            return (T)_values[pName].Peek();
        }

        public T GetValueOrDefault<T>(string pName)
        {
            if (HasValue(pName)) return GetValue<T>(pName);
            return default;
        }

        public bool HasValue(string pName)
        {
            return _values.ContainsKey(pName);
        }

        public void RemoveValue(string pName)
        {
            if (_values[pName].Count > 1) _values[pName].Pop();
            else _values.Remove(pName);
        }
    }
}
