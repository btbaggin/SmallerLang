﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using SmallerLang.Emitting;

namespace SmallerLang.Syntax
{
    public class ForSyntax : SyntaxNode
    {
        public IdentifierSyntax Iterator { get; private set; }

        public IList<DeclarationSyntax> Initializer { get; private set; }

        public ExpressionSyntax Condition { get; private set; }

        public IList<ExpressionSyntax> Finalizer { get; private set; }

        public BlockSyntax Body { get; private set; }

        public override SmallType Type => SmallTypeCache.Undefined;

        internal ForSyntax(IdentifierSyntax pIterator, BlockSyntax pBody)
        {
            Iterator = pIterator;
            Body = pBody;
        }

        internal ForSyntax(IList<DeclarationSyntax> pInitializer, ExpressionSyntax pCondition, IList<ExpressionSyntax> pFinalizer, BlockSyntax pBody)
        {
            Initializer = pInitializer;
            Condition = pCondition;
            Finalizer = pFinalizer;
            Body = pBody;
        }

        public override LLVMValueRef Emit(EmittingContext pContext)
        {
            //Iterator should have been rewritten to normal for loop
            System.Diagnostics.Debug.Assert(Iterator == null);

            pContext.Locals.AddScope();

            foreach (var d in Initializer)
            {
                d.Emit(pContext);
            }

            //for condition
            var cond = Condition.Emit(pContext);
            LLVMValueRef cmp = SmallTypeCache.GetLLVMDefault(Condition.Type, pContext);

            LLVMValueRef condv;
            if (Condition.Type.IsFloat())
            {
                condv = LLVM.BuildFCmp(pContext.Builder, LLVMRealPredicate.LLVMRealONE, cond, cmp, "whilecond");
            }
            else
            {
                condv = LLVM.BuildICmp(pContext.Builder, LLVMIntPredicate.LLVMIntNE, cond, cmp, "whilecond");
            }

            var loop = LLVM.AppendBasicBlock(pContext.CurrentMethod, "loop");
            var end = LLVM.AppendBasicBlock(pContext.CurrentMethod, "loopend");

            //Jump to end or loop
            LLVM.BuildCondBr(pContext.Builder, condv, loop, end);

            //Loop
            LLVM.PositionBuilderAtEnd(pContext.Builder, loop);
            Body.Emit(pContext);
            foreach (var f in Finalizer)
            {
                f.Emit(pContext);
            }

            //Jump back to start
            cond = Condition.Emit(pContext);
            condv = LLVM.BuildICmp(pContext.Builder, LLVMIntPredicate.LLVMIntNE, cond, cmp, "whilecond");
            LLVM.BuildCondBr(pContext.Builder, condv, loop, end);

            //End
            LLVM.PositionBuilderAtEnd(pContext.Builder, end);
            
            pContext.Locals.RemoveScope();

            return default;
        }
    }
}
