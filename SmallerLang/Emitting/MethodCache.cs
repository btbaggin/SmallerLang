using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;
using System.Diagnostics;

namespace SmallerLang.Emitting
{
    public struct MethodDefinition
    {
        public string MangledName { get; private set; }
        public string Name { get; private set; }
        public bool External { get; private set; }
        public List<SmallType> ArgumentTypes { get; private set; }
        public SmallType ReturnType { get; private set; }

        public MethodDefinition(string pName, string pMangled, bool pExternal, List<SmallType> pArguments, SmallType pReturn)
        {
            Name = pName;
            MangledName = pMangled;
            External = pExternal;
            ArgumentTypes = pArguments;
            ReturnType = pReturn;
        }

        public MethodDefinition(string pName, List<SmallType> pArguments)
        {
            Name = pName;
            MangledName = pName;
            ArgumentTypes = pArguments;
            ReturnType = SmallTypeCache.Undefined;
            External = false;
        }

        public MethodDefinition MakeConcreteDefinition(SmallType pType)
        {
            //Not a generic type... don't need to worry about it
            if (pType == null || !pType.HasGenericArguments) return this;

            //Transform the generic parameters and return types to match the concrete type
            List<SmallType> arguments = new List<SmallType>(ArgumentTypes.Count);

            Dictionary<string, int> typeIndexes = new Dictionary<string, int>(pType.GenericParameters.Count);
            for(int i = 0; i < pType.GenericParameters.Count; i++)
            {
                typeIndexes.Add(pType.GenericParameters[i], i);
            }

            foreach (var a in ArgumentTypes)
            {
                if (a.IsGenericParameter && typeIndexes.ContainsKey(a.Name))
                {
                    var i = typeIndexes[a.Name];
                    arguments.Add(pType.GenericArguments[i]);
                }
                else if (!a.IsGenericParameter)
                {
                    arguments.Add(a);
                }
            }

            List<SmallType> returnTypes = new List<SmallType>();
            if (ReturnType.IsTuple)
            {
                foreach (var f in ReturnType.GetFields())
                {
                    if (f.Type.IsGenericParameter && typeIndexes.ContainsKey(f.Type.Name))
                    {
                        var i = typeIndexes[f.Type.Name];
                        returnTypes.Add(pType.GenericArguments[i]);
                    }
                    else if (!f.Type.IsGenericParameter)
                    {
                        returnTypes.Add(f.Type);
                    }
                }
            }
            else if (ReturnType.IsGenericParameter && typeIndexes.ContainsKey(ReturnType.Name))
            {
                var i = typeIndexes[ReturnType.Name];
                returnTypes.Add(pType.GenericArguments[i]);
            }
            else if (!ReturnType.IsGenericParameter) returnTypes.Add(ReturnType);

            SmallType ret = SmallTypeCache.GetOrCreateTuple(returnTypes.ToArray());

            return new MethodDefinition(Name, MangledName, External, arguments, ret);
        }

        public override string ToString()
        {
            var name = new StringBuilder();
            name.Append(Name);
            name.Append("(");

            foreach (var p in ArgumentTypes)
            {
                name.Append(p.ToString());
                name.Append(",");
            }
            if (ArgumentTypes.Count > 0) name = name.Remove(name.Length - 1, 1);

            name.Append(")");

            if (ReturnType != SmallTypeCache.Undefined)
            {
                name.Append(" -> ");
                name.Append(ReturnType.ToString());
            }

            return name.ToString();
        }
    }

    public partial class MethodCache
    {
        public string Namespace { get; private set; }
        readonly Dictionary<string, List<MethodDefinition>> _methods;
        readonly Dictionary<string, int> _counter;
        internal const string CAST_METHOD = "<cast>"; //Use a normally invalid method name so we don't get collisions

        public MethodCache()
        {
            _methods = new Dictionary<string, List<MethodDefinition>>();
            _counter = new Dictionary<string, int>();
        }

        public MethodDefinition AddMethod(string pName, MethodSyntax pNode)
        {
            return AddMethod(null, pName, pNode);
        }

        public MethodDefinition AddMethod(SmallType pType, string pName, MethodSyntax pNode)
        {
            var name = GetMethodName(pType, pName);
            if (!_methods.ContainsKey(name))
            {
                _methods.Add(name, new List<MethodDefinition>());
                _counter.Add(name, 0);
            }
            _counter[name]++;
            var md = GetDefinition(pNode, _counter[name], name);
            _methods[name].Add(md);
            return md;
        }

        public bool MethodExists(SmallType pType, string pName, MethodSyntax pNode)
        {
            var name = GetMethodName(pType, pName);

            if (_methods.ContainsKey(name))
            {
                SmallType[] types = Utils.SyntaxHelper.SelectNodeTypes(pNode.Parameters);
                return FindMethod(out MethodDefinition m, out bool e, pType, name, types);
            }

            return false;
        }

        internal bool FindCast(SmallType pFromType, SmallType pToType, out MethodDefinition pDefinition)
        {
            pDefinition = default;
            var name = GetMethodName(null, CAST_METHOD);
            if (_methods.ContainsKey(name))
            {
                bool found = false;
                foreach (var md in _methods[name])
                {
                    if (pFromType == md.ArgumentTypes[0] && md.ReturnType == pFromType)
                    {
                        pDefinition = md;
                        return true;
                    }

                    if (pFromType.IsAssignableFrom(md.ArgumentTypes[0]) && md.ReturnType == pToType)
                    {
                        pDefinition = md;
                        found = true;
                    }
                }

                return found;
            }

            return false;
        }

        public bool FindMethod(out MethodDefinition pMethod, out bool pExact, SmallType pType, string pName, params SmallType[] pArguments)
        {
            var name = GetMethodName(pType, pName);
            if (!_methods.ContainsKey(name))
            {
                pMethod = default;
                pExact = false;
                return false;
            }

            List<MethodDefinition> candidates = _methods[name];
            MethodDefinition retval = candidates[0];
            foreach (var c in candidates)
            {
                //Parameter count match
                if (c.ArgumentTypes.Count == pArguments.Length)
                {
                    retval = c;
                    //Types match
                    bool found = true;
                    for (int i = 0; i < c.ArgumentTypes.Count && found; i++)
                    {
                        if(c.ArgumentTypes[i].IsGenericParameter && pType != null)
                        {
                            found = pArguments[i].IsAssignableFrom(pType.GenericArguments[i]);
                        }
                        else
                        {
                            found = pArguments[i].IsAssignableFrom(c.ArgumentTypes[i]);
                        }
                    }                   

                    if (found)
                    {
                        pMethod = c;
                        pExact = true;
                        return true;
                    }
                }
            }

            pMethod = retval;
            pExact = false;
            return false;
        }

        public MethodSyntax MatchMethod(MethodCallSyntax pCallSite, IEnumerable<MethodSyntax> pMethods)
        {
            var arguments = Utils.SyntaxHelper.SelectNodeTypes(pCallSite.Arguments);

            foreach(var m in pMethods)
            {
                var parameters = Utils.SyntaxHelper.SelectNodeTypes(m.Parameters);
                if(arguments.Length == parameters.Length)
                {
                    bool found = true;
                    for (int i = 0; i < arguments.Length && found; i++)
                    {
                        found = arguments[i].IsAssignableFrom(parameters[i]);
                    }

                    if (found) return m;
                }
            }

            return null;
        }

        public IList<MethodDefinition> GetAllMatches(string pName, int pParmCount)
        {
            List<MethodDefinition> retval = new List<MethodDefinition>();
            pName = GetMethodName(null, pName);
            if (!_methods.ContainsKey(pName))
            {
                return retval;
            }

            foreach (var c in _methods[pName])
            {
                //Parameter count match
                if (c.ArgumentTypes.Count == pParmCount)
                {
                    retval.Add(c);
                }
            }

            return retval;
        }

        private string GetMethodName(SmallType pType, string pMethod)
        {
            string type = null;
            if (Namespace != null) type += Namespace + "___";
            if (pType != null) type += pType.Name + "___";
            return type + pMethod;
        }
    }
}
