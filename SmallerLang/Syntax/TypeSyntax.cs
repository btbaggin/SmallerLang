using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Emitting;

namespace SmallerLang.Syntax
{
    public class TypeSyntax : SyntaxNode
    {
        public override SmallType Type => SmallTypeCache.FromString(Value);

        public override SyntaxType SyntaxType => SyntaxType.Type;

        public IList<TypeSyntax> GenericArguments { get; private set; }

        public string Value { get; private set; }

        internal TypeSyntax(string pValue, IList<TypeSyntax> pGenericArgs)
        {
            Value = pValue;
            GenericArguments = pGenericArgs;
        }

        public override LLVMSharp.LLVMValueRef Emit(EmittingContext pContext)
        {
            //No generation for type syntax
            throw new NotImplementedException();
        }
    }
}
