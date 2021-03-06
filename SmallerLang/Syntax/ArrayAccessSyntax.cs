﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using SmallerLang.Emitting;

namespace SmallerLang.Syntax
{
    public class ArrayAccessSyntax : IdentifierSyntax
    {
        public SmallType BaseType => base.Type;

        public override SmallType Type => base.Type.GetElementType();

        public override SyntaxType SyntaxType => SyntaxType.ArrayAccess;

        public IdentifierSyntax Identifier { get; private set; }

        public SyntaxNode Index { get; private set; }

        public ArrayAccessSyntax(IdentifierSyntax pVariable, SyntaxNode pIndex) : base(pVariable.Value)
        {
            Identifier = pVariable;
            Index = pIndex;
        }

        public override LLVMValueRef Emit(EmittingContext pContext)
        {
            pContext.EmitDebugLocation(this);

            //We are in a member access, just push the index of this field onto the stack
            LLVMValueRef variable = Identifier.Emit(pContext);
            var index = Index.Emit(pContext);

            Utils.LlvmHelper.LoadIfPointer(ref index, pContext);

            var indexAccess = LLVM.BuildInBoundsGEP(pContext.Builder, variable, new LLVMValueRef[] { pContext.GetInt(0), pContext.GetInt(1) }, "arrayaccess");
            var load = LLVM.BuildLoad(pContext.Builder, indexAccess, "");

            return LLVM.BuildInBoundsGEP(pContext.Builder, load, new LLVMValueRef[] { index }, "arrayaccess");
        }

        public override T FromNode<T>(T pNode)
        {
            //TODO This is really weird and hacky
            //It's like this to turn ItSyntax into ArrayAccessSyntax
            if (pNode is ArrayAccessSyntax a)
            {
                var n = (IdentifierSyntax)(SyntaxNode)base.FromNode(pNode);
                n.SetType(a.BaseType);
            }

            return (T)(SyntaxNode)this;
            
        }
    }
}
