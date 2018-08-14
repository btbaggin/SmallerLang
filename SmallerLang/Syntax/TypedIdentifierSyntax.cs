using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Emitting;

namespace SmallerLang.Syntax
{
    public class TypedIdentifierSyntax : SyntaxNode
    {
        public string Value { get; private set; }

        private readonly TypeSyntax _type;
        public override SmallType Type
        {
            get { return _type.Type; }
        }

        internal TypedIdentifierSyntax(TypeSyntax pType, string pValue)
        {
            Value = pValue;
            _type = pType;
        }

        public override LLVMSharp.LLVMValueRef Emit(EmittingContext pContext)
        {
            throw new NotImplementedException();
        }
    }
}
