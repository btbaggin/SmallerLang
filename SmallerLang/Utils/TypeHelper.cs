using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmallerLang.Utils
{
    static class TypeHelper
    {
        public static bool IsFloat(SmallType pType)
        {
            return pType == SmallTypeCache.Float || pType == SmallTypeCache.Double;
        }

        public static bool IsInt(SmallType pType)
        {
            return pType == SmallTypeCache.Short || pType == SmallTypeCache.Int || pType == SmallTypeCache.Long;
        }

        public static bool IsNumber(SmallType pType)
        {
            return pType == SmallTypeCache.Float || pType == SmallTypeCache.Double || pType == SmallTypeCache.Int ||
                   pType== SmallTypeCache.Long || pType== SmallTypeCache.Short;
        }

        public static string GetDefaultConstructorName(SmallType pType)
        {
            return pType.GetFullName() + ".ctor";
        }
    }
}
