using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using SmallerLang.Emitting;

namespace SmallerLang.Syntax
{
    public class CaseSyntax : SyntaxNode
    {
        public List<SyntaxNode> Conditions { get; private set; }

        public BlockSyntax Body { get; private set; }

        public bool IsDefault => Conditions.Count == 0;

        public override SmallType Type => SmallTypeCache.Undefined;

        public override SyntaxType SyntaxType => SyntaxType.Case;

        internal CaseSyntax(List<SyntaxNode> pConditions, BlockSyntax pBody)
        {
            Conditions = pConditions;
            Body = pBody;
        }

        public override LLVMValueRef Emit(EmittingContext pContext)
        {
            pContext.EmitDebugLocation(this);

            //Most of the code for case is generated in SelectSyntax
            var b = LLVM.AppendBasicBlock(pContext.CurrentMethod, "case");
            LLVM.PositionBuilderAtEnd(pContext.Builder, b);
            Body.Emit(pContext);
            return b;
        }
    }
}
