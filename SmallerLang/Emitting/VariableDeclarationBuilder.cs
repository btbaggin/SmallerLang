using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;

namespace SmallerLang.Emitting
{
    public class VariableDeclarationBuilder : IDisposable
    {
        public LLVMBuilderRef Builder { get; private set; }
        public VariableDeclarationBuilder(EmittingContext pContext)
        {
            Builder = LLVM.CreateBuilder();
            LLVM.PositionBuilder(Builder, pContext.CurrentMethod.GetEntryBasicBlock(), pContext.CurrentMethod.GetEntryBasicBlock().GetFirstInstruction());
        }

        public void Dispose()
        {
            LLVM.DisposeBuilder(Builder);
        }
    }
}
