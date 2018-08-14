using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SmallerLang.Parser
{
    [Serializable]
    public class ParseException : Exception
    {
        public ParseException(string pMessage) : base(pMessage) { }

        protected ParseException(SerializationInfo pInfo, StreamingContext pContext) : base(pInfo, pContext) { }
    }
}
