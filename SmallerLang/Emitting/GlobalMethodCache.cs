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
        private static Dictionary<string, MethodCache> _namespaces = new Dictionary<string, MethodCache>();

        public static MethodCache CreateNamespace(string pNamespace)
        {
            if (!_namespaces.ContainsKey(pNamespace)) _namespaces.Add(pNamespace, new MethodCache(pNamespace));
            return _namespaces[pNamespace];
        }

        public static bool FindMethod(out MethodDefinition pMethod, string pNamespace, SmallType pType, string pName, params SmallType[] pArguments)
        {
            if(!_namespaces.ContainsKey(pNamespace))
            {
                pMethod = default;
                return false;
            }
            return _namespaces[pNamespace].FindMethod(out pMethod, out bool pExact, pType, pName, pArguments);
        }

        public static bool FindMethod(out MethodDefinition pMethod, out bool pExact, string pNamespace, SmallType pType, string pName, params SmallType[] pArguments)
        {
            if (!_namespaces.ContainsKey(pNamespace))
            {
                pMethod = default;
                pExact = false;
                return false;
            }
            return _namespaces[pNamespace].FindMethod(out pMethod, out pExact, pType, pName, pArguments);
        }

        internal static string GetMangledName(string pNamespace, SmallType pType, string pName, params SmallType[] pArguments)
        {
            Debug.Assert(FindMethod(out MethodDefinition m, pNamespace, pType, pName, pArguments));
            return m.MangledName;
        }

        public static bool FindCast(SmallType pFromType, SmallType pToType, out MethodDefinition pDefinition)
        {
            pDefinition = default;
            foreach(var n in _namespaces.Values)
            {
                if (n.CastExists(pFromType, pToType, out pDefinition)) return true;
            }
            return false;
        }

        public static bool CastExists(SmallType pFromType, SmallType pToType)
        {
            return FindCast(pFromType, pToType, out MethodDefinition pDef);
        }

        private static MethodDefinition GetDefinition(MethodSyntax pMethod, int pCounter, string pName)
        {
            List<SmallType> arguments = new List<SmallType>();
            for (int i = 0; i < pMethod.Parameters.Count; i++)
            {
                arguments.Add(pMethod.Parameters[i].Type);
            }

            SmallType ret = pMethod.Type;
            string mangledName = pMethod.External ? pName : pName + "_" + pCounter;
            return new MethodDefinition(pMethod.Name, mangledName, arguments, ret);
        }
    }  
}
