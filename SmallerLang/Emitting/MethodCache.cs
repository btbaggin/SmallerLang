using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }

    public static class MethodCache
    {
        readonly static Dictionary<string, List<MethodDefinition>> _methods = new Dictionary<string, List<MethodDefinition>>();
        readonly static Dictionary<string, int> _counter = new Dictionary<string, int>();

        public static void AddMethod(string pName, Syntax.MethodSyntax pNode)
        {
            if (!_methods.ContainsKey(pNode.Name))
            {
                _methods.Add(pNode.Name, new List<MethodDefinition>());
                _counter.Add(pNode.Name, 0);
            }
            _counter[pName]++;
            _methods[pName].Add(GetDefinition(pNode, _counter[pName]));
        }

        public static bool MethodExists(string pName, Syntax.MethodSyntax pNode)
        {
            if(_methods.ContainsKey(pNode.Name))
            {
                SmallType[] types = Utils.SyntaxHelper.SelectNodeTypes(pNode.Parameters);
                return FindMethod(out MethodDefinition? m, pName, types);
            }

            return false;
        }

        public static bool CastExists(SmallType pFromType, SmallType pToType, out MethodDefinition pDefinition)
        {
            pDefinition = default;
            if(_methods.ContainsKey("cast"))
            {
                bool found = false;
                foreach (var md in _methods["cast"])
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
            if (_methods.ContainsKey("cast"))
            {
                int count = 0;
                foreach (var md in _methods["cast"])
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

        public static bool FindMethod(out MethodDefinition? pMethod, string pName, params SmallType[] pType)
        {
            if (!_methods.ContainsKey(pName))
            {
                pMethod = null;
                return false;
            }

            List<MethodDefinition> candidates = _methods[pName];
            MethodDefinition retval = candidates[0];
            foreach (var c in candidates)
            {
                //Parameter count match
                if (c.ArgumentTypes.Count == pType.Length)
                {
                    retval = c;
                    //Types match
                    bool found = true;
                    for (int i = 0; i < c.ArgumentTypes.Count && found; i++)
                    {
                        found = c.ArgumentTypes[i].IsAssignableFrom(pType[i]);
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

        public static string GetMangledName(string pName, params SmallType[] pType)
        {
            System.Diagnostics.Debug.Assert(FindMethod(out MethodDefinition? m, pName, pType));
            return m.Value.MangledName;
        }

        private static MethodDefinition GetDefinition(Syntax.MethodSyntax pMethod, int pCounter)
        {
            List<SmallType> arguments = new List<SmallType>();
            for (int i = 0; i < pMethod.Parameters.Count; i++)
            {
                arguments.Add(pMethod.Parameters[i].Type);
            }
            SmallType ret = pMethod.Type;
            string mangledName = pMethod.External ? pMethod.Name : pMethod.Name + "_" + pCounter;
            return new MethodDefinition(pMethod.Name, mangledName, arguments, ret);
        }
    }
}
