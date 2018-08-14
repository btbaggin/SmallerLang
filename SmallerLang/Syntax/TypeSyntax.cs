using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Emitting;

namespace SmallerLang.Syntax
{
    public class TypeSyntax : ExpressionSyntax
    {
        public override SmallType Type => SmallTypeCache.FromString(_typeString);

        public IList<TypeSyntax> GenericParameters { get; private set; }

        private readonly string _typeString;
        internal TypeSyntax(string pValue, IList<TypeSyntax> pGenericArgs)
        {
            _typeString = pValue;
            GenericParameters = pGenericArgs;
        }

        public override LLVMSharp.LLVMValueRef Emit(EmittingContext pContext)
        {
            //No generation for type syntax
            throw new NotImplementedException();
        }
    }
}
