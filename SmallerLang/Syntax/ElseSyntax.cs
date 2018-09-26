using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Emitting;

namespace SmallerLang.Syntax
{
    public class ElseSyntax : SyntaxNode
    {
        public BlockSyntax Body { get; private set; }

        public IfSyntax If { get; private set; }

        public override SmallType Type => SmallTypeCache.Undefined;

        public ElseSyntax(BlockSyntax pBody, IfSyntax pIf)
        {
            Body = pBody;
            If = pIf;
        }

        public override LLVMSharp.LLVMValueRef Emit(EmittingContext pContext)
        {
            pContext.EmitDebugLocation(this);

            If?.Emit(pContext);
            Body?.Emit(pContext);
            return default;
        }
    }
}
