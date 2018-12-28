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

        public override SyntaxType SyntaxType => SyntaxType.MemberAccess;

        internal MemberAccessSyntax(IdentifierSyntax pIdentifier, IdentifierSyntax pValue) : base(pIdentifier.Value)
        {
            Identifier = pIdentifier;
            Value = pValue;
        }

        public override LLVMValueRef Emit(EmittingContext pContext)
        {
            pContext.EmitDebugLocation(this);

            string ns = "";
            string type = Identifier.Value;
            if (Identifier.SyntaxType == SyntaxType.Namespace)
            {
                ns = Identifier.Value;
                type = Value.Value;
            }

            //Check if this is a "static" method
            if (!pContext.Cache.IsTypeDefined(type))//TODO namespace
            {
                LLVMValueRef value;
                if (Identifier.SyntaxType == SyntaxType.Namespace)
                {
                    value = Value.Emit(pContext);
                    //Terminal nodes are fully emitted in their child most node
                    if (IsTerminalNode(Value))
                    {
                        value = AccessStack<MemberAccess>.BuildGetElementPtr(pContext, value);
                    }
                }
                else
                {
                    LLVMValueRef identifier = Identifier.Emit(pContext);

                    //For every method call we need to stop and allocate a new variable
                    //This is because our method call is going to return a value, but we need a pointer.
                    //To solve this we allocate a temporary variable, store the value, and continue
                    if (Identifier.SyntaxType == SyntaxType.MethodCall)
                    {
                        var tempVar = pContext.AllocateVariable("memberaccess_temp", Identifier.Type);
                        LLVM.BuildStore(pContext.Builder, identifier, tempVar);
                        identifier = tempVar;
                        pContext.AccessStack.Clear();
                    }

                    //Store the index we pushed at.
                    //We will later pop only if that index still exists
                    //This allows things like MethodCalls to wipe the stack (because the arguments aren't in the same stack) while still properly popping values
                    var index = pContext.AccessStack.Push(new MemberAccess(identifier, Identifier.Type));

                    value = Value.Emit(pContext);
                    //Terminal nodes are fully emitted in their child most node
                    if (IsTerminalNode(Value))
                    {
                        value = AccessStack<MemberAccess>.BuildGetElementPtr(pContext, value);
                    }

                    pContext.AccessStack.PopFrom(index);
                }
                
                return value;
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
            return pNode.SyntaxType != SyntaxType.MethodCall && pNode.SyntaxType != SyntaxType.MemberAccess;
        }
    }
}
