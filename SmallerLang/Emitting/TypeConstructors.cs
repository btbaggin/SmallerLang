using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using SmallerLang.Syntax;

namespace SmallerLang.Emitting
{
    static class TypeConstructors
    {
        public const string StringFromCharsName = "string.ctor";

        public static void StringFromChars(LLVMValueRef pVariable, SyntaxNode pChars, EmittingContext pContext)
        {
            var data = pChars.Emit(pContext);

            //Save length
            var arrayLength = pContext.GetArrayLength(data);
            var stringLength = pContext.GetArrayLength(pVariable);

            LLVM.BuildStore(pContext.Builder, LLVM.BuildLoad(pContext.Builder, arrayLength, ""), stringLength);

            var arrayData = LLVM.BuildInBoundsGEP(pContext.Builder, data, new LLVMValueRef[] { pContext.GetInt(0), pContext.GetInt(1) }, "");
            arrayData = LLVM.BuildLoad(pContext.Builder, arrayData, "");

            //Load the data
            var variableData = LLVM.BuildInBoundsGEP(pContext.Builder, pVariable, new LLVMValueRef[] { pContext.GetInt(0), pContext.GetInt(1) }, "");
            LLVM.BuildStore(pContext.Builder, arrayData, variableData);
        }
    }
}
