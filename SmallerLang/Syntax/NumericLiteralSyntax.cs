using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Emitting;

namespace SmallerLang.Syntax
{
    public enum NumberTypes
    {
        Short,
        Integer,
        Long,
        Double,
        Float
    }

    public class NumericLiteralSyntax : IdentifierSyntax
    {
        public NumberTypes NumberType { get; internal set; }

        public override SmallType Type
        {
            get
            {
                switch(NumberType)
                {
                    case NumberTypes.Double:
                        return SmallTypeCache.Double;
                    case NumberTypes.Float:
                        return SmallTypeCache.Float;
                    case NumberTypes.Integer:
                        return SmallTypeCache.Int;
                    case NumberTypes.Long:
                        return SmallTypeCache.Long;
                    case NumberTypes.Short:
                        return SmallTypeCache.Short;
                    default:
                        throw new NotSupportedException("Unknown NumberType " + NumberType.ToString());
                }
            }
        }

        public override SyntaxType SyntaxType => SyntaxType.NumericLiteral;

        internal NumericLiteralSyntax(string pValue, NumberTypes pType) : base(pValue)
        {
            NumberType = pType;
        }

        public override LLVMSharp.LLVMValueRef Emit(EmittingContext pContext)
        {
            switch(NumberType)
            {
                case NumberTypes.Integer:
                    return pContext.GetInt(int.Parse(Value));
                case NumberTypes.Long:
                    return pContext.GetLong(long.Parse(Value));
                case NumberTypes.Short:
                    return pContext.GetShort(short.Parse(Value));
                case NumberTypes.Float:
                    return pContext.GetFloat(float.Parse(Value));
                case NumberTypes.Double:
                    return pContext.GetDouble(double.Parse(Value));
                default:
                    throw new NotSupportedException();
            }
        }
    }
}
