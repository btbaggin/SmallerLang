using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;

namespace SmallerLang.Emitting
{
    public readonly struct LocalDefinition
    {
        public bool IsConst { get; }
        public bool IsParameter { get; }
        public LLVMValueRef Value { get; }
        public SmallType Type { get; }

        public LocalDefinition(bool pIsParameter, bool pIsConst, SmallType pType) : this(pIsParameter, pIsConst, default, pType) { }

        public LocalDefinition(bool pIsParameter, bool pIsConst, LLVMValueRef pValue, SmallType pType)
        {
            IsConst = pIsConst;
            IsParameter = pIsParameter;
            Value = pValue;
            Type = pType;
        }

        public static LocalDefinition CreateParameter(LLVMValueRef pValue, SmallType pType)
        {
            return new LocalDefinition(true, false, pValue, pType);
        }

        public static LocalDefinition Create(bool pIsConst, SmallType pType)
        {
            return new LocalDefinition(false, pIsConst, pType);
        }

        public static LocalDefinition Create(LLVMValueRef pValue, SmallType pType)
        {
            return new LocalDefinition(false, false, pValue, pType);
        }
    }

    public class ScopeCache<T>
    {
        private Dictionary<string, T>[] _variables;
        private int _scopeCount;

        public ScopeCache()
        {
            _variables = new Dictionary<string, T>[32];
            _scopeCount = -1;
        }

        public void SetValue(string pName, T pT)
        {
            System.Diagnostics.Debug.Assert(_scopeCount >= 0, "No scope has been added");
            for (int i = _scopeCount; i >= 0; i--)
            {
                if (_variables[i].ContainsKey(pName))
                {
                    _variables[i][pName] = pT;
                }
            }
        }

        public IEnumerable<T> GetVariablesInScope()
        {
            System.Diagnostics.Debug.Assert(_scopeCount >= 0, "No scope has been added");
            foreach(var kv in _variables[_scopeCount])
            {
                yield return kv.Value;
            }
        }

        public bool IsVariableDefined(string pName)
        {
            System.Diagnostics.Debug.Assert(_scopeCount >= 0, "No scope has been added");
            for(int i = 0; i <= _scopeCount; i++)
            {
                if(_variables[i].ContainsKey(pName))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsVariableDefinedInScope(string pName)
        {
            //Checks if the variable is defined in the current scope, ignoring all other scopes
            System.Diagnostics.Debug.Assert(_scopeCount >= 0, "No scope has been added");
            return _variables[_scopeCount].ContainsKey(pName);
        }

        public void DefineVariableInScope(string pName, T pT)
        {
            System.Diagnostics.Debug.Assert(_scopeCount >= 0, "No scope has been added");
            _variables[_scopeCount].Add(pName, pT);
        }

        public T GetVariable(string pName)
        {
            //We have to look from the most recent scope to the oldest
            System.Diagnostics.Debug.Assert(_scopeCount >= 0, "No scope has been added");
            for (int i = _scopeCount; i >= 0; i--)
            {
                if (_variables[i].ContainsKey(pName))
                {
                    return _variables[i][pName];
                }
            }

            throw new InvalidOperationException("Variable " + pName + " not defined in any scope");
        }

        public bool TryGetVariable(string pName, out T pT)
        {

            System.Diagnostics.Debug.Assert(_scopeCount >= 0, "No scope has been added");
            if (pName == null)
            {
                pT = default;
                return false;
            }

            for (int i = _scopeCount; i >= 0; i--)
            {
                if (_variables[i].ContainsKey(pName))
                {
                    pT = _variables[i][pName];
                    return true;
                }
            }

            pT = default;
            return false;
        }

        public void AddScope()
        {
            if (_scopeCount >= _variables.Length)
            {
                Array.Resize(ref _variables, _variables.Length * 2);
            }

            _scopeCount++;
            _variables[_scopeCount] = new Dictionary<string, T>();
        }

        public void RemoveScope()
        {
            System.Diagnostics.Debug.Assert(_scopeCount >= 0, "No scope has been added");
            _variables[_scopeCount] = null;
            _scopeCount--;
        }

        public ScopeCache<T> Copy()
        {
            var copy = new ScopeCache<T>()
            {
                _scopeCount = _scopeCount
            };
            Array.Copy(_variables, copy._variables, _variables.Length);
            return copy;
        }
    }
}
