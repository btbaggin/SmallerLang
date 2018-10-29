using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using SmallerLang.Emitting;

namespace SmallerLang.Syntax
{
    public class ReturnSyntax : SyntaxNode
    {
        public IList<SyntaxNode> Values { get; private set; }

        public override SmallType Type
        {
            get
            {
                if (Values.Count == 0) return SmallTypeCache.Undefined;
                if (Values.Count == 1) return Values[0].Type;
                SmallType[] types = new SmallType[Values.Count];
                for (int i = 0; i < types.Length; i++)
                {
                    types[i] = Values[i].Type;
                }
                return SmallTypeCache.GetOrCreateTuple(types);
            }
        }

        public override SyntaxType SyntaxType => SyntaxType.Return;

        internal ReturnSyntax(IList<SyntaxNode> pValues)
        {
            Values = pValues;
        }

        public override LLVMValueRef Emit(EmittingContext pContext)
        {
            pContext.EmitDebugLocation(this);

            //Emit deferred statements right before return
            foreach (var s in pContext.GetDeferredStatements())
            {
                s.Emit(pContext);
            }

            //We want to dispose variables after deferred statements because
            //then variables referenced in deferred statements will still be valid
            BlockSyntax.BuildCallToDispose(pContext);

            LLVMValueRef v;
            if (Values.Count == 1)
            {
                v = Values[0].Emit(pContext);
                Utils.LlvmHelper.LoadIfPointer(ref v, pContext);
            }
            else
            {
                //Allocate our tuple and set each field
                v = LLVM.BuildAlloca(pContext.Builder, SmallTypeCache.GetLLVMType(Type, pContext), "");
                for(int i = 0; i < Values.Count; i++)
                {
                    var location = LLVM.BuildInBoundsGEP(pContext.Builder, v, new LLVMValueRef[] { pContext.GetInt(0), pContext.GetInt(i) }, "");
                    var value = Values[i].Emit(pContext);
                    Utils.LlvmHelper.LoadIfPointer(ref value, pContext);
                    LLVM.BuildStore(pContext.Builder, value, location);
                }
                v = LLVM.BuildLoad(pContext.Builder, v, "");
            }
            return LLVM.BuildRet(pContext.Builder, v);
        }
    }
}
