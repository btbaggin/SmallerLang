using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;

namespace SmallerLang.Utils
{
    static class LlvmHelper
    {
        public static bool IsFloat(LLVMValueRef pValue)
        {
            var tk = pValue.TypeOf().TypeKind;
            return tk == LLVMTypeKind.LLVMFloatTypeKind || tk == LLVMTypeKind.LLVMDoubleTypeKind;
        }

        public static bool IsPointer(LLVMValueRef pValue)
        {
            return pValue.TypeOf().TypeKind == LLVMTypeKind.LLVMPointerTypeKind;
        }

        public static void MakePointer(LLVMValueRef pValue, ref LLVMTypeRef pType)
        {
            if (IsPointer(pValue))
            {
                pType = LLVMTypeRef.PointerType(pType, 0);
            }
        }

        public static void LoadIfPointer(ref LLVMValueRef pValue, Emitting.EmittingContext pContext)
        {
            if (IsPointer(pValue))
            {
                pValue = LLVM.BuildLoad(pContext.Builder, pValue, "");
            }
        }
    }
}
