using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using SmallerLang.Emitting;

namespace SmallerLang.Syntax
{
    public class SelectSyntax : SyntaxNode
    {
        public SyntaxNode Condition { get; private set; }

        public IList<CaseSyntax> Cases { get; private set; }

        public override SmallType Type => SmallTypeCache.Undefined;

        public override SyntaxType SyntaxType => SyntaxType.Select;

        internal SelectSyntax(SyntaxNode pCondition, IList<CaseSyntax> pCases)
        {
            Condition = pCondition;
            Cases = pCases;
        }

        public override LLVMValueRef Emit(EmittingContext pContext)
        {
            var cond = Condition.Emit(pContext);

            var endBlock = LLVM.AppendBasicBlock(pContext.CurrentMethod, "endswitch");
            LLVMBasicBlockRef elseBlock;

            var builder = pContext.GetTempBuilder();
            //Default case has to be the last one
            if(Cases[Cases.Count - 1].IsDefault)
            {
                //Emit else into our else block
                var b = pContext.Builder;
                pContext.SetBuilder(builder);
                elseBlock = Cases[Cases.Count - 1].Emit(pContext);
                pContext.SetBuilder(b);
            }
            else
            {
                //No else, so this is just an empty block
                elseBlock = LLVM.AppendBasicBlock(pContext.CurrentMethod, "else");
            }
            LLVM.PositionBuilderAtEnd(builder, elseBlock);

            var s = LLVM.BuildSwitch(pContext.Builder, cond, elseBlock, (uint)Cases.Count);
            for (int i = 0; i < Cases.Count; i++)
            {
                if(!Cases[i].IsDefault)
                {
                    //Emit case and jump to end
                    var c = Cases[i].Emit(pContext);
                    LLVM.BuildBr(pContext.Builder, endBlock);

                    foreach(var cs in Cases[i].Conditions)
                    {
                        LLVM.AddCase(s, cs.Emit(pContext), c);
                    }
                }
            }
            LLVM.PositionBuilderAtEnd(pContext.Builder, endBlock);

            //Jump from our else block to the end
            LLVM.BuildBr(builder, endBlock);
            LLVM.DisposeBuilder(builder);
            return default;
        }
    }
}
