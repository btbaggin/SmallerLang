using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using SmallerLang.Emitting;

namespace SmallerLang.Syntax
{
    public class TernaryExpressionSyntax : SyntaxNode
    {
        SmallType _type;
        public override SmallType Type => _type;

        public override SyntaxType SyntaxType => SyntaxType.TernaryExpression;

        public SyntaxNode Condition { get; private set; }

        public SyntaxNode Left { get; private set; }

        public SyntaxNode Right { get; private set; }

        internal TernaryExpressionSyntax(SyntaxNode pCondition, SyntaxNode pOpTrue, SyntaxNode pOpFalse)
        {
            Condition = pCondition;
            Left = pOpTrue;
            Right = pOpFalse;
        }

        public override LLVMValueRef Emit(EmittingContext pContext)
        {
            pContext.EmitDebugLocation(this);
            var variable = pContext.AllocateVariable("tern_return", Left.Type);

            var cond = Condition.Emit(pContext);
            Utils.LlvmHelper.LoadIfPointer(ref cond, pContext);

            var trueBranch = LLVM.AppendBasicBlock(pContext.CurrentMethod, "tern_true");
            LLVMBasicBlockRef falseBranch = LLVM.AppendBasicBlock(pContext.CurrentMethod, "tern_false");
            var end = LLVM.AppendBasicBlock(pContext.CurrentMethod, "tern_end");

            //Jump to true or false
            LLVM.BuildCondBr(pContext.Builder, cond, trueBranch, falseBranch);

            //Emit true value
            LLVM.PositionBuilderAtEnd(pContext.Builder, trueBranch);
            LLVM.BuildStore(pContext.Builder, Left.Emit(pContext), variable);

            //Jump to end
            LLVM.BuildBr(pContext.Builder, end);

            //Emit false value
            LLVM.PositionBuilderAtEnd(pContext.Builder, falseBranch);
            LLVM.BuildStore(pContext.Builder, Right.Emit(pContext), variable);

            //Jump to end
            LLVM.BuildBr(pContext.Builder, end);

            LLVM.PositionBuilderAtEnd(pContext.Builder, end);

            return variable;
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
