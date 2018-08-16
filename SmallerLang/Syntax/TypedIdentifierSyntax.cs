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

        public TypeSyntax TypeNode { get; private set; }

        public override SmallType Type
        {
            get { return TypeNode.Type; }
        }

        internal TypedIdentifierSyntax(TypeSyntax pType, string pValue)
        {
            Value = pValue;
            TypeNode = pType;
        }

        public override LLVMSharp.LLVMValueRef Emit(EmittingContext pContext)
        {
            throw new NotImplementedException();
        }
    }
}
