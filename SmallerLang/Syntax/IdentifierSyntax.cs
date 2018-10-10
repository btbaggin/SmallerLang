using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Emitting;

namespace SmallerLang.Syntax
{
    public class IdentifierSyntax : SyntaxNode
    {
        internal bool DoNotLoad { get; set; }

        public string Value { get; private set; }

        private SmallType _type = SmallTypeCache.Undefined;
        public override SmallType Type
        {
            get { return _type; }
        }

        public override SyntaxType SyntaxType => SyntaxType.Identifier;

        internal IdentifierSyntax(string pValue)
        {
            Value = pValue;
        }

        public override LLVMSharp.LLVMValueRef Emit(EmittingContext pContext)
        {
            pContext.EmitDebugLocation(this);

            //We are in a member access, just push the index of this field onto the stack
            if(pContext.AccessStack.Count > 0)
            {
                var idx = pContext.AccessStack.Peek().Type.GetFieldIndex(Value);
                return pContext.GetInt(idx);
            }

            System.Diagnostics.Debug.Assert(pContext.Locals.IsVariableDefined(Value), "Variable " + Value + " not defined in scope");

            var ld = pContext.Locals.GetVariable(Value);

            if (ld.IsParameter || Type.IsStruct || Type.IsArray || Type.IsTrait || DoNotLoad) return ld.Value;
            return LLVMSharp.LLVM.BuildLoad(pContext.Builder, ld.Value, Value);
        }

        public void SetType(SmallType pType)
        {
            _type = pType;
        }

        public override T FromNode<T>(T pNode)
        {
            _type = pNode.Type;
            return base.FromNode(pNode);
        }
    }
}
