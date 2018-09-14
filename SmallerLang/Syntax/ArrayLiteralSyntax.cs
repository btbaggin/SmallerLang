using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using SmallerLang.Emitting;

namespace SmallerLang.Syntax
{
    public class ArrayLiteralSyntax : IdentifierSyntax
    {
        public TypeSyntax TypeNode { get; private set; }

        public override SmallType Type
        {
            get { return TypeNode.Type.MakeArrayType(); }
        }

        public int Size
        {
            get
            {
                int.TryParse(Value, out int i);
                return i;
            }
        }

        public ArrayLiteralSyntax(TypeSyntax pType, string pValue) : base(pValue)
        {
            TypeNode = pType; 
        }

        public override LLVMValueRef Emit(EmittingContext pContext)
        {
            var v = pContext.AllocateVariable("array_temp", Type);

            var a = LLVM.BuildInBoundsGEP(pContext.Builder, v, new LLVMValueRef[] { pContext.GetInt(0), pContext.GetInt(0) }, "");
            LLVM.BuildStore(pContext.Builder, pContext.GetInt(int.Parse(Value)), a);

            var arraya = LLVM.BuildAlloca(pContext.Builder, LLVMTypeRef.ArrayType(SmallTypeCache.GetLLVMType(Type.GetElementType()), (uint)Size), "");
            var arrayaccess = LLVM.BuildInBoundsGEP(pContext.Builder, arraya, new LLVMValueRef[] { pContext.GetInt(0), pContext.GetInt(0) }, "");
            var d = LLVM.BuildInBoundsGEP(pContext.Builder, v, new LLVMValueRef[] { pContext.GetInt(0), pContext.GetInt(1) }, "");
            LLVM.BuildStore(pContext.Builder, arrayaccess, d);

            return v;
        }
    }
}
