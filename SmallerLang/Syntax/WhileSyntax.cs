using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Emitting;

namespace SmallerLang.Syntax
{
    public class WhileSyntax : SyntaxNode
    {
        public ExpressionSyntax Condition { get; private set; }

        public BlockSyntax Body { get; private set; }

        public override SmallType Type => SmallTypeCache.Undefined;

        public WhileSyntax(ExpressionSyntax pCondition, BlockSyntax pBody)
        {
            Condition = pCondition;
            Body = pBody;
        }

        public override LLVMSharp.LLVMValueRef Emit(EmittingContext pContext)
        {
            pContext.EmitDebugLocation(this);

            //If condition
            var cond = Condition.Emit(pContext);
            var loop = LLVMSharp.LLVM.AppendBasicBlock(pContext.CurrentMethod, "while_loop");
            var end = LLVMSharp.LLVM.AppendBasicBlock(pContext.CurrentMethod, "while_end");

            //Jump to end or loop
            LLVMSharp.LLVM.BuildCondBr(pContext.Builder, cond, loop, end);

            //Loop
            LLVMSharp.LLVM.PositionBuilderAtEnd(pContext.Builder, loop);
            Body.Emit(pContext);

            if(!Utils.SyntaxHelper.LastStatementIsReturn(Body))
            {
                //Jump back to start
                cond = Condition.Emit(pContext);
                LLVMSharp.LLVM.BuildCondBr(pContext.Builder, cond, loop, end);
            }

            //End
            LLVMSharp.LLVM.PositionBuilderAtEnd(pContext.Builder, end);

            return default;
        }
    }
}
