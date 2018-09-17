using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;

namespace SmallerLang.Emitting
{
    public class VariableCache<T>
    {
        private Dictionary<string, (T TItem, bool IsParameter)>[] _variables;
        private int _scopeCount;

        public VariableCache()
        {
            _variables = new Dictionary<string, (T, bool)>[128];
            _scopeCount = -1;
        }

        public void SetVariableValue(string pName, T pValue)
        {
            for (int i = _scopeCount; i >= 0; i--)
            {
                if (_variables[i].ContainsKey(pName))
                {
                    bool parm = _variables[i][pName].IsParameter;
                    _variables[i][pName] = (pValue, parm);
                }
            }
        }

        public IEnumerable<(string Variable, T Value)> GetVariablesInScope()
        {
            foreach(var kv in _variables[_scopeCount])
            {
                yield return (kv.Key, kv.Value.TItem);
            }
        }

        public bool IsVariableDefined(string pName)
        {
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
            return _variables[_scopeCount].ContainsKey(pName);
        }

        public void DefineVariableInScope(string pName, T pValue)
        {
            _variables[_scopeCount].Add(pName, (pValue, false));
        }

        public void DefineParameter(string pName, T pValue)
        {
            _variables[_scopeCount].Add(pName, (pValue, true));
        }

        public T GetVariable(string pName)
        {
            return GetVariable(pName, out bool p);
        }

        public T GetVariable(string pName, out bool pParameter)
        {
            //We have to look from the most recent scope to the oldest
            for (int i = _scopeCount; i >= 0; i--)
            {
                if (_variables[i].ContainsKey(pName))
                {
                    pParameter = _variables[i][pName].IsParameter;
                    return _variables[i][pName].TItem;
                }
            }

            throw new InvalidOperationException("Variable " + pName + " not defined in any scope");
        }

        public void AddScope()
        {
            if (_scopeCount >= _variables.Length)
            {
                Array.Resize(ref _variables, _variables.Length * 2);
            }

            _scopeCount++;
            _variables[_scopeCount] = new Dictionary<string, (T, bool)>();
        }

        public void RemoveScope()
        {
            _variables[_scopeCount] = null;
            _scopeCount--;
        }

        public VariableCache<T> Copy()
        {
            var copy = new VariableCache<T>
            {
                _scopeCount = _scopeCount
            };
            Array.Copy(_variables, copy._variables, _variables.Length);
            return copy;
        }
    }
}
