using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Emitting;

namespace SmallerLang.Syntax
{
    public class IdentifierSyntax : ExpressionSyntax
    {
        public string Value { get; private set; }

        private SmallType _type = SmallTypeCache.Undefined;
        public override SmallType Type
        {
            get { return _type; }
        }

        internal IdentifierSyntax(string pValue)
        {
            Value = pValue;
        }

        public override LLVMSharp.LLVMValueRef Emit(EmittingContext pContext)
        {
            //We are in a member access, just push the index of this field onto the stack
            if(pContext.AccessStack.Count > 0)
            {
                var i = pContext.AccessStack.Peek().Type.GetFieldIndex(Value);
                return pContext.GetInt(i);
            }

            System.Diagnostics.Debug.Assert(pContext.Locals.IsVariableDefined(Value), "Variable " + Value + " not defined in scope");

            var v = pContext.Locals.GetVariable(Value, out bool parameter);

            if (parameter || Type.IsStruct ||Type.IsArray) return v;
            return LLVMSharp.LLVM.BuildLoad(pContext.Builder, v, Value);
        }

        public void SetType(SmallType pType)
        {
            _type = pType;
        }

        public override SyntaxNode FromNode(SyntaxNode pNode)
        {
            _type = pNode.Type;
            return base.FromNode(pNode);
        }
    }
}
