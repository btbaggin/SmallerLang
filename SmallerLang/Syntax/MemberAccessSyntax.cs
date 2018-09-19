using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using SmallerLang.Emitting;

namespace SmallerLang.Syntax
{
    public class MemberAccessSyntax : IdentifierSyntax
    {
        public IdentifierSyntax Identifier { get; private set; }

        public new IdentifierSyntax Value { get; private set; }

        public override SmallType Type => Value.Type;

        internal MemberAccessSyntax(IdentifierSyntax pIdentifier, IdentifierSyntax pValue) : base(pIdentifier.Value)
        {
            Identifier = pIdentifier;
            Value = pValue;
        }

        public override LLVMValueRef Emit(EmittingContext pContext)
        {
            //Check if this is a "static" method
            if(!SmallTypeCache.IsTypeDefined(Identifier.Value))
            {
                LLVMValueRef i = Identifier.Emit(pContext);
                pContext.AccessStack.Push(i, Identifier.Type);
                    
                LLVMValueRef v = Value.Emit(pContext);
                //Terminal nodes are fully emitted in their child most node
                if(IsTerminalNode(Value))
                {
                    v = MemberAccessStack.BuildGetElementPtr(pContext, v);
                }

                pContext.AccessStack.Pop();
                return v;
            }
            else
            {
                //Only this way while fields are allow to be accessed
                if(Identifier.Type.IsEnum)
                {
                    var i = Identifier.Type.GetEnumValue(Value.Value);
                    return pContext.GetInt(i);
                }

                throw new NotSupportedException();
            }
        }

        private bool IsTerminalNode(SyntaxNode pNode)
        {
            return pNode.GetType() != typeof(MethodCallSyntax) && pNode.GetType() != typeof(MemberAccessSyntax);
        }
    }
}
