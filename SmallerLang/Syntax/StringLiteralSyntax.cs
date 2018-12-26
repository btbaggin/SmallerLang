using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using SmallerLang.Emitting;

namespace SmallerLang.Syntax
{
    public class StringLiteralSyntax : IdentifierSyntax
    {
        public override SmallType Type => SmallTypeCache.String;

        public override SyntaxType SyntaxType => SyntaxType.StringLiteral;

        internal StringLiteralSyntax(string pValue) : base(pValue) { }

        public override LLVMValueRef Emit(EmittingContext pContext)
        {
            pContext.EmitDebugLocation(this);

            var variable = pContext.AllocateVariable("string_temp", this);

            //Save length
            var length = LLVM.BuildInBoundsGEP(pContext.Builder, variable, new LLVMValueRef[] { pContext.GetInt(0), pContext.GetInt(0) }, "");
            LLVM.BuildStore(pContext.Builder, pContext.GetInt(Value.Length), length);

            //Allocate space for our string
            var tempBuilder = pContext.GetTempBuilder();
            LLVM.PositionBuilder(tempBuilder, pContext.CurrentMethod.GetEntryBasicBlock(), pContext.CurrentMethod.GetEntryBasicBlock().GetFirstInstruction());

            var dataArray = LLVM.BuildAlloca(tempBuilder, LLVMTypeRef.ArrayType(SmallTypeCache.GetLLVMType(SmallTypeCache.Char, pContext), (uint)(Value.Length + 1)), "");
            LLVM.DisposeBuilder(tempBuilder);

            //Store the string constant in the array
            var data = pContext.GetString(Value);
            LLVM.BuildStore(pContext.Builder, data, dataArray);

            //Load the length and data
            var dataAccess = LLVM.BuildInBoundsGEP(pContext.Builder, dataArray, new LLVMValueRef[] { pContext.GetInt(0), pContext.GetInt(0) }, "");
            var variableData = LLVM.BuildInBoundsGEP(pContext.Builder, variable, new LLVMValueRef[] { pContext.GetInt(0), pContext.GetInt(1) }, "");
            LLVM.BuildStore(pContext.Builder, dataAccess, variableData);

            return variable;
        }
    }
}
