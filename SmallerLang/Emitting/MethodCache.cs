using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;

namespace SmallerLang.Emitting
{
    public struct MethodDefinition
    {
        public string MangledName { get; private set; }
        public string Name { get; private set; }
        public List<SmallType> ArgumentTypes { get; private set; }
        public SmallType ReturnType { get; private set; }

        public MethodDefinition(string pName, string pMangled, List<SmallType> pArguments, SmallType pReturn)
        {
            Name = pName;
            MangledName = pMangled;
            ArgumentTypes = pArguments;
            ReturnType = pReturn;
        }

        public MethodDefinition(string pName)
        {
            Name = pName;
            MangledName = pName;
            ArgumentTypes = new List<SmallType>();
            ReturnType = SmallTypeCache.Undefined;
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

    public static class MethodCache
    {
        readonly static Dictionary<string, List<MethodDefinition>> _methods = new Dictionary<string, List<MethodDefinition>>();
        readonly static Dictionary<string, int> _counter = new Dictionary<string, int>();
        internal const string CAST_METHOD = "<cast>"; //Use a normally invalid method name so we don't get collisions

        public static MethodDefinition AddMethod(string pName, MethodSyntax pNode)
        {
            return AddMethod(null, pName, pNode);
        }

        public static MethodDefinition AddMethod(SmallType pType, string pName, MethodSyntax pNode)
        {
            var name = GetMethodName(pType, pName);
            if (!_methods.ContainsKey(name))
            {
                _methods.Add(name, new List<MethodDefinition>());
                _counter.Add(name, 0);
            }
            _counter[name]++;
            var md = GetDefinition(pNode, _counter[name], pType);
            _methods[name].Add(md);
            return md;
        }

        public static bool MethodExists(string pName, MethodSyntax pNode)
        {
            return MethodExists(null, pName, pNode);
        }

        public static bool MethodExists(SmallType pType, string pName, MethodSyntax pNode)
        {
            var name = GetMethodName(pType, pName);

            if (_methods.ContainsKey(name))
            {
                SmallType[] types = Utils.SyntaxHelper.SelectNodeTypes(pNode.Parameters);
                return FindMethod(out MethodDefinition m, name, types);
            }

            return false;
        }
        
        public static bool CastExists(SmallType pFromType, SmallType pToType, out MethodDefinition pDefinition)
        {
            pDefinition = default;
            if(_methods.ContainsKey(CAST_METHOD))
            {
                bool found = false;
                foreach (var md in _methods[CAST_METHOD])
                {
                    if(pFromType == md.ArgumentTypes[0] && md.ReturnType == pFromType)
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

        internal static int CastCount(SmallType pFromType, SmallType pToType)
        {
            if (_methods.ContainsKey(CAST_METHOD))
            {
                int count = 0;
                foreach (var md in _methods[CAST_METHOD])
                {
                    if (md.ArgumentTypes[0] == pFromType && md.ReturnType == pToType)
                    {
                        count++;
                    }
                }
                return count;
            }

            return 0;
        }

        public static bool FindMethod(out MethodDefinition pMethod, string pName, params SmallType[] pArguments)
        {
            return FindMethod(out pMethod, null, pName, pArguments);
        }

        public static bool FindMethod(out MethodDefinition pMethod, SmallType pType, string pName, params SmallType[] pArguments)
        {
            var name = GetMethodName(pType, pName);
            if (!_methods.ContainsKey(name))
            {
                pMethod = default;
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
                        found = c.ArgumentTypes[i].IsAssignableFrom(pArguments[i]);
                    }                   

                    if (found)
                    {
                        pMethod = c;
                        return true;
                    }
                }
            }

            pMethod = retval;
            return false;
        }

        public static MethodSyntax MatchMethod(MethodCallSyntax pCallSite, IEnumerable<MethodSyntax> pMethods)
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

        public static IList<MethodDefinition> GetAllMatches(string pName, int pParmCount)
        {
            List<MethodDefinition> retval = new List<MethodDefinition>();
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

        public static string GetMangledName(string pName, SmallType pType, params SmallType[] pArguments)
        {
            System.Diagnostics.Debug.Assert(FindMethod(out MethodDefinition m, pType, pName, pArguments));
            return m.MangledName;
        }

        private static string GetMethodName(SmallType pType, string pMethod)
        {
            if (pType == null) return pMethod;
            return pType.Name + "___" + pMethod;
        }

        private static MethodDefinition GetDefinition(Syntax.MethodSyntax pMethod, int pCounter, SmallType pInstanceType)
        {
            string name = GetMethodName(pInstanceType, pMethod.Name);

            List<SmallType> arguments = new List<SmallType>();
            for (int i = 0; i < pMethod.Parameters.Count; i++)
            {
                arguments.Add(pMethod.Parameters[i].Type);
            }

            SmallType ret = pMethod.Type;
            string mangledName = pMethod.External ? name : name + "_" + pCounter;
            return new MethodDefinition(pMethod.Name, mangledName, arguments, ret);
        }
    }
}
