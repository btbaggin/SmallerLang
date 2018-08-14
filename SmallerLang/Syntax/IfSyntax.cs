using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Emitting;

namespace SmallerLang.Syntax
{
    public class IfSyntax : SyntaxNode
    {
        public ExpressionSyntax Condition { get; private set; }

        public BlockSyntax Body { get; private set; }

        public ElseSyntax Else { get; private set; }

        public override SmallType Type => SmallTypeCache.Undefined;

        internal IfSyntax(ExpressionSyntax pCondition, BlockSyntax pBody, ElseSyntax  pElse)
        {
            Condition = pCondition;
            Body = pBody;
            Else = pElse;
        }

        public override LLVMSharp.LLVMValueRef Emit(EmittingContext pContext)
        {
            var cond = Condition.Emit(pContext);
            var then = LLVMSharp.LLVM.AppendBasicBlock(pContext.CurrentMethod, "then");

            LLVMSharp.LLVMBasicBlockRef e = default;
            if (Else != null)
            {
               e = LLVMSharp.LLVM.AppendBasicBlock(pContext.CurrentMethod, "else");
            }
            var end = LLVMSharp.LLVM.AppendBasicBlock(pContext.CurrentMethod, "ifend");

            //Jump to if or else
            LLVMSharp.LLVM.BuildCondBr(pContext.Builder, cond, then, Else != null ? e : end);

            //Emit then value
            LLVMSharp.LLVM.PositionBuilderAtEnd(pContext.Builder, then);
            Body.Emit(pContext);

            if(!Utils.SyntaxHelper.LastStatementIsReturn(Body))
            {
                //Jump to end only if we didn't terminate in the body
                LLVMSharp.LLVM.BuildBr(pContext.Builder, end);
            }

            //Emit else value
            if(Else != null)
            {
                LLVMSharp.LLVM.PositionBuilderAtEnd(pContext.Builder, e);
                Else.Emit(pContext);

                if(!Utils.SyntaxHelper.LastStatementIsReturn(Else))
                {
                    //Jump to end only if we didn't terminate in the body
                    LLVMSharp.LLVM.BuildBr(pContext.Builder, end);
                }
            }

            LLVMSharp.LLVM.PositionBuilderAtEnd(pContext.Builder, end);

            return default;
        }
    }
}
