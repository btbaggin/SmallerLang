﻿using System;
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

            LLVMValueRef v;
            if (Values.Count == 1)
            {
                v = Values[0].Emit(pContext);

                //Emit deferred statements right before return
                foreach (var s in pContext.GetDeferredStatements())
                {
                    s.Emit(pContext);
                }
                Utils.LlvmHelper.LoadIfPointer(ref v, pContext);
            }
            else
            {
                //Allocate our tuple and set each field
                v = LLVM.BuildAlloca(pContext.Builder, SmallTypeCache.GetLLVMType(Type), "");
                for(int i = 0; i < Values.Count; i++)
                {
                    var value = LLVM.BuildInBoundsGEP(pContext.Builder, v, new LLVMValueRef[] { pContext.GetInt(0), pContext.GetInt(i) }, "");
                    LLVM.BuildStore(pContext.Builder, Values[i].Emit(pContext), value);
                }
                v = LLVM.BuildLoad(pContext.Builder, v, "");
            }
            return LLVM.BuildRet(pContext.Builder, v);
        }
    }
}
