using System;
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

        public bool Reverse { get; private set; }

        public SyntaxNode Condition { get; private set; }

        public IList<SyntaxNode> Finalizer { get; private set; }

        public BlockSyntax Body { get; private set; }

        public override SmallType Type => SmallTypeCache.Undefined;

        public override SyntaxType SyntaxType => SyntaxType.For;

        internal ForSyntax(IdentifierSyntax pIterator, bool pBackwards, BlockSyntax pBody)
        {
            Iterator = pIterator;
            Reverse = pBackwards;
            Body = pBody;
        }

        internal ForSyntax(IList<DeclarationSyntax> pInitializer, SyntaxNode pCondition, IList<SyntaxNode> pFinalizer, BlockSyntax pBody)
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
            if (Utils.TypeHelper.IsFloat(Condition.Type))
            {
                condv = LLVM.BuildFCmp(pContext.Builder, LLVMRealPredicate.LLVMRealONE, cond, cmp, "for_cond");
            }
            else
            {
                condv = LLVM.BuildICmp(pContext.Builder, LLVMIntPredicate.LLVMIntNE, cond, cmp, "for_cond");
            }

            var loop = LLVM.AppendBasicBlock(pContext.CurrentMethod, "for_body");
            var end = LLVM.AppendBasicBlock(pContext.CurrentMethod, "for_end");

            pContext.BreakLocations.Push(end);

            //Jump to end or loop
            LLVM.BuildCondBr(pContext.Builder, condv, loop, end);

            //Loop
            LLVM.PositionBuilderAtEnd(pContext.Builder, loop);
            Body.Emit(pContext);
            foreach (var f in Finalizer)
            {
                f.Emit(pContext);
            }

            pContext.BreakLocations.Pop();

            //Jump back to start
            cond = Condition.Emit(pContext);
            condv = LLVM.BuildICmp(pContext.Builder, LLVMIntPredicate.LLVMIntNE, cond, cmp, "for_cond");
            LLVM.BuildCondBr(pContext.Builder, condv, loop, end);

            //End
            LLVM.PositionBuilderAtEnd(pContext.Builder, end);
            
            pContext.Locals.RemoveScope();

            return default;
        }
    }
}
