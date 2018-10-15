using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;

namespace SmallerLang.Emitting
{
    public struct LocalDefinition
    {
        public string Name { get; private set; }
        public bool IsParameter { get; private set; }
        public LLVMValueRef Value { get; private set; }
        public SmallType Type { get; private set; }
        public bool IsReferenced { get; set; }

        public LocalDefinition(string pName, bool pIsParameter, LLVMValueRef pValue, SmallType pType)
        {
            Name = pName;
            IsParameter = pIsParameter;
            Value = pValue;
            Type = pType;
            IsReferenced = false;
        }
    }

    public class VariableCache
    {
        private Dictionary<string, LocalDefinition>[] _variables;
        private int _scopeCount;

        public VariableCache()
        {
            _variables = new Dictionary<string, LocalDefinition>[128];
            _scopeCount = -1;
        }

        public void SetVariableReferenced(string pName)
        {
            for (int i = _scopeCount; i >= 0; i--)
            {
                if (_variables[i].ContainsKey(pName))
                {
                    var ld = _variables[i][pName];
                    ld.IsReferenced = true;
                    _variables[i][pName] = ld;
                }
            }
        }

        public IEnumerable<LocalDefinition> GetVariablesInScope()
        {
            foreach(var kv in _variables[_scopeCount])
            {
                yield return kv.Value;
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

        public void DefineVariableInScope(string pName, SmallType pType, LLVMValueRef pValue)
        {
            var ld = new LocalDefinition(pName, false, pValue, pType);
            _variables[_scopeCount].Add(pName, ld);
        }

        public void DefineVariableInScope(string pName, SmallType pType)
        {
            DefineVariableInScope(pName, pType, default);
        }

        public void DefineParameter(string pName, SmallType pType, LLVMValueRef pValue)
        {
            var ld = new LocalDefinition(pName, true, pValue, pType);
            _variables[_scopeCount].Add(pName, ld);
        }

        public LocalDefinition GetVariable(string pName)
        {
            //We have to look from the most recent scope to the oldest
            for (int i = _scopeCount; i >= 0; i--)
            {
                if (_variables[i].ContainsKey(pName))
                {
                    return _variables[i][pName];
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
            _variables[_scopeCount] = new Dictionary<string, LocalDefinition>();
        }

        public void RemoveScope()
        {
            _variables[_scopeCount] = null;
            _scopeCount--;
        }

        public VariableCache Copy()
        {
            var copy = new VariableCache()
            {
                _scopeCount = _scopeCount
            };
            Array.Copy(_variables, copy._variables, _variables.Length);
            return copy;
        }
    }
}
