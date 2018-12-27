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
        public static bool CastExists(SmallType pFromType, SmallType pToType, out MethodDefinition pDefinition)
        {
            pDefinition = default;
            //TODO
            //foreach(var ns in NamespaceManager.GetAllNamespaces())
            //{
            //    if (ns.FindCast(out pDefinition, pFromType, pToType)) return true;
            //}
            return false;
        }

        public static bool CastExists(SmallType pFromType, SmallType pToType)
        {
            return CastExists(pFromType, pToType, out MethodDefinition pDef);
        }

        private static MethodDefinition GetDefinition(MethodSyntax pMethod, int pCounter, string pName)
        {
            List<SmallType> arguments = new List<SmallType>();
            for (int i = 0; i < pMethod.Parameters.Count; i++)
            {
                var parmType = pMethod.Parameters[i].Type;
                arguments.Add(parmType);
            }

            SmallType ret = pMethod.Type;
            string mangledName = pMethod.External ? pName : pName + "_" + pCounter;
            return new MethodDefinition(pMethod.Name, mangledName, pMethod.External, arguments, ret);
        }
    }  
}
