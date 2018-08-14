using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmallerLang.Utils
{
    static class LlvmHelper
    {
        public static bool IsPointer(LLVMSharp.LLVMValueRef pValue)
        {
            return pValue.TypeOf().TypeKind == LLVMSharp.LLVMTypeKind.LLVMPointerTypeKind;
        }

        public static void MakePointer(LLVMSharp.LLVMValueRef pValue, ref LLVMSharp.LLVMTypeRef pType)
        {
            if (IsPointer(pValue))
            {
                pType = LLVMSharp.LLVMTypeRef.PointerType(pType, 0);
            }
        }

        public static void LoadIfPointer(ref LLVMSharp.LLVMValueRef pValue, Emitting.EmittingContext pContext)
        {
            if (IsPointer(pValue))
            {
                pValue = LLVMSharp.LLVM.BuildLoad(pContext.Builder, pValue, "");
            }
        }
    }
}
