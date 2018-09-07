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
                pContext.AccessStack.Push(new MemberAccessItem(i, Identifier.Type));
                    
                LLVMValueRef v = Value.Emit(pContext);
                //Method calls and nested member access will be taken care of by their child most node
                if(Value.GetType() != typeof(MethodCallSyntax) &&
                    Value.GetType() != typeof(MemberAccessSyntax))
                {
                    List<LLVMValueRef> indexes = new List<LLVMValueRef>(pContext.AccessStack.Count);
                    indexes.Add(pContext.GetInt(0));

                    for (int j = pContext.AccessStack.Count - 1; j > 0; j--)
                    {
                        indexes.Add(pContext.AccessStack.PeekAt(j).Value);
                    }
                    indexes.Add(v);
                    i = pContext.AccessStack.PeekAt(0).Value;
                    v = LLVM.BuildInBoundsGEP(pContext.Builder, i, indexes.ToArray(), "field_" + Value.Value);
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
    }
}
