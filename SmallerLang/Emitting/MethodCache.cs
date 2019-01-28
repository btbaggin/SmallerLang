using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;
using System.Diagnostics;

namespace SmallerLang.Emitting
{
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

        public MethodDefinition AddMethod(SmallType pType, string pNamespace, MethodSyntax pNode)
        {
            var name = GetMethodName(pType, pNode.Name);
            if (!_methods.ContainsKey(name))
            {
                _methods.Add(name, new List<MethodDefinition>());
                _counter.Add(name, 0);
            }
            _counter[name]++;
            var md = GetDefinition(pNode, _counter[name], pNamespace, name);
            _methods[name].Add(md);
            return md;
        }

        public Compiler.FindResult MethodExists(SmallType pType, MethodSyntax pNode)
        {
            var name = GetMethodName(pType, pNode.Name);

            if (_methods.ContainsKey(name))
            {
                SmallType[] types = Utils.SyntaxHelper.SelectNodeTypes(pNode.Parameters);
                return FindMethod(out MethodDefinition m, false, pType, name, types);
            }

            return Compiler.FindResult.NotFound;
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

        public Compiler.FindResult FindMethod(out MethodDefinition pMethod, bool pAllowPrivate, SmallType pType, string pName, params SmallType[] pArguments)
        {
            var name = GetMethodName(pType, pName);
            if (!_methods.ContainsKey(name))
            {
                pMethod = default;
                return Compiler.FindResult.NotFound;
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

                    //Scope match
                    if (found)
                    {
                        pMethod = c;
                        if (pMethod.Scope == FileScope.Public || pAllowPrivate) return Compiler.FindResult.Found;
                        return Compiler.FindResult.IncorrectScope;
                    }
                }
            }

            pMethod = retval;
            return Compiler.FindResult.NotFound;
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

        public IList<MethodDefinition> GetPossibleMatches(string pName, int pParmCount)
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

        private static MethodDefinition GetDefinition(MethodSyntax pMethod, int pCounter, string pNamespace, string pName)
        {
            List<SmallType> arguments = new List<SmallType>(pMethod.Parameters.Count);
            for (int i = 0; i < pMethod.Parameters.Count; i++)
            {
                var parmType = pMethod.Parameters[i].Type;
                arguments.Add(parmType);
            }

            SmallType ret = pMethod.Type;
            string mangledName = pNamespace + "__" + pName + "_" + pCounter;
            return new MethodDefinition(pMethod.Scope, pMethod.Name, mangledName, pMethod.External, arguments, ret);
        }
    }
}
