using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using SmallerLang.Emitting;

namespace SmallerLang.Syntax
{
    public class BreakSyntax : SyntaxNode
    {
        public override SmallType Type => SmallTypeCache.Undefined;

        public override SyntaxType SyntaxType => SyntaxType.Break;

        public string Count { get; private set; }

        public int CountAsInt
        {
            get
            {
                if (string.IsNullOrEmpty(Count)) return 0;
                return int.Parse(Count);
            }
        }

        internal BreakSyntax(string pCount)
        {
            Count = pCount;
        }

        public override LLVMValueRef Emit(EmittingContext pContext)
        {
            System.Diagnostics.Debug.Assert(pContext.BreakLocations.Count > 0);


            return LLVM.BuildBr(pContext.Builder, pContext.BreakLocations.PeekAt(pContext.BreakLocations.Count - CountAsInt - 1));
        }
    }
}
