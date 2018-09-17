using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using SmallerLang.Emitting;

namespace SmallerLang.Syntax
{
    public class ArrayAccessSyntax : IdentifierSyntax
    {
        public SmallType BaseType => base.Type;

        public override SmallType Type => base.Type.GetElementType();

        public ExpressionSyntax Index { get; private set; }

        public ArrayAccessSyntax(string pVariable, ExpressionSyntax pIndex) : base(pVariable)
        {
            Index = pIndex;
        }

        public override LLVMValueRef Emit(EmittingContext pContext)
        {
            //We are in a member access, just push the index of this field onto the stack
            LLVMValueRef variable;
            List<LLVMValueRef> indexes = new List<LLVMValueRef>
            {
                pContext.GetInt(0)
            };

            MemberAccessStack member = null;
            if (pContext.AccessStack.Count == 0)
            {
                System.Diagnostics.Debug.Assert(pContext.Locals.IsVariableDefined(Value));
                variable = pContext.Locals.GetVariable(Value);
            }
            else
            {
                var idx = pContext.AccessStack.Peek().Type.GetFieldIndex(Value);
                //Save the current stack so we can restore it when we are done
                //We clear this because we are no longer in a member access when emitting the index
                member = pContext.AccessStack.Copy();
                //"consume" the entire access stack to get the object we are indexing on
                variable = MemberAccessStack.BuildGetElementPtr(pContext, pContext.GetInt(idx));
                pContext.AccessStack.Clear();
            }

            indexes.Add(pContext.GetInt(1));
            var index = Index.Emit(pContext);

            Utils.LlvmHelper.LoadIfPointer(ref index, pContext);

            var indexAccess = LLVM.BuildInBoundsGEP(pContext.Builder, variable, indexes.ToArray(), "arrayaccess");
            var load = LLVM.BuildLoad(pContext.Builder, indexAccess, "");

            if (member != null) pContext.AccessStack = member;

            return LLVM.BuildInBoundsGEP(pContext.Builder, load, new LLVMValueRef[] { index }, "arrayaccess");
        }
    }
}
