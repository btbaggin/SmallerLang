﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Emitting;

namespace SmallerLang.Syntax
{
    public class BlockSyntax : SyntaxNode
    {
        public IList<SyntaxNode> Statements { get; private set; }

        public override SmallType Type => SmallTypeCache.Undefined;

        public BlockSyntax(IList<SyntaxNode> pStatements)
        {
            Statements = pStatements;
        }

        public override LLVMSharp.LLVMValueRef Emit(EmittingContext pContext)
        {
            pContext.Locals.AddScope();
            pContext.AddDebugScope(Span);

            foreach(var s in Statements)
            {
                if (!s.Deferred) s.Emit(pContext);
                else pContext.AddDeferredStatement(s);
            }

            pContext.RemoveDebugScope();
            pContext.Locals.RemoveScope();
            return default;
        }
    }
}
