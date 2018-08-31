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
            if(!SmallTypeCache.IsTypeDefined(Identifier.Value))
            {
                var i = Identifier.Emit(pContext);
                pContext.MemberAccessStack.Push(i);

                LLVMValueRef v;
                if(Value.GetType() == typeof(MethodCallSyntax))
                {
                    v = Value.Emit(pContext);
                }
                else
                {
                    int f = Identifier.Type.GetFieldIndex(Value.Value);
                    v = LLVM.BuildInBoundsGEP(pContext.Builder, i, new LLVMValueRef[] { pContext.GetInt(0), pContext.GetInt(f) }, "field_" + Value.Value);
                }

                pContext.MemberAccessStack.Pop();
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
