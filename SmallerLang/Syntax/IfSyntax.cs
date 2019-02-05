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
        public SyntaxNode Condition { get; private set; }

        public BlockSyntax Body { get; private set; }

        public ElseSyntax Else { get; private set; }

        public override SmallType Type => SmallTypeCache.Undefined;

        public override SyntaxType SyntaxType => SyntaxType.If;

        internal IfSyntax(SyntaxNode pCondition, BlockSyntax pBody, ElseSyntax  pElse)
        {
            Condition = pCondition;
            Body = pBody;
            Else = pElse;
        }

        public override LLVMSharp.LLVMValueRef Emit(EmittingContext pContext)
        {
            pContext.EmitDebugLocation(this);

            var cond = Condition.Emit(pContext);
            Utils.LlvmHelper.LoadIfPointer(ref cond, pContext);
            var then = LLVMSharp.LLVM.AppendBasicBlock(pContext.CurrentMethod, "if_then");

            LLVMSharp.LLVMBasicBlockRef elseB = default;
            if (Else != null)
            {
               elseB = LLVMSharp.LLVM.AppendBasicBlock(pContext.CurrentMethod, "if_else");
            }
            var endB = LLVMSharp.LLVM.AppendBasicBlock(pContext.CurrentMethod, "if_end");

            //Jump to if or else/end (depending on if we have an else block)
            LLVMSharp.LLVM.BuildCondBr(pContext.Builder, cond, then, Else != null ? elseB : endB);

            //Emit then value
            LLVMSharp.LLVM.PositionBuilderAtEnd(pContext.Builder, then);
            Body.Emit(pContext);

            if(!Utils.SyntaxHelper.LastStatementIsReturn(Body) && !Utils.SyntaxHelper.LastStatementIsBreak(Body))
            {
                //Jump to end only if we didn't terminate in the body
                LLVMSharp.LLVM.BuildBr(pContext.Builder, endB);
            }

            //Emit else value
            if(Else != null)
            {
                LLVMSharp.LLVM.PositionBuilderAtEnd(pContext.Builder, elseB);
                Else.Emit(pContext);

                if(!Utils.SyntaxHelper.LastStatementIsReturn(Else))
                {
                    //Jump to end only if we didn't terminate in the body
                    LLVMSharp.LLVM.BuildBr(pContext.Builder, endB);
                }
            }

            //Position builder at the end of the if statement 
            //before we continue on to emitting other statements
            LLVMSharp.LLVM.PositionBuilderAtEnd(pContext.Builder, endB);
            return default;
        }
    }
}
