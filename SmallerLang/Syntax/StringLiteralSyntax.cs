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
            var length = pContext.GetArrayLength(variable);
            LLVM.BuildStore(pContext.Builder, pContext.GetInt(Value.Length), length);

            //Allocate space for our string
            LLVMValueRef dataArray;
            using (var b = new VariableDeclarationBuilder(pContext))
            {
                dataArray = LLVM.BuildAlloca(b.Builder, LLVMTypeRef.ArrayType(SmallTypeCache.GetLLVMType(SmallTypeCache.Char, pContext), (uint)(Value.Length + 1)), ""); //Need to allocate 1 more space for the /0
            }

            //Store the string constant in the allocated array
            var data = pContext.GetString(Value);
            LLVM.BuildStore(pContext.Builder, data, dataArray);

            //Store the allocated array in the string variable
            var dataAccess = LLVM.BuildInBoundsGEP(pContext.Builder, dataArray, new LLVMValueRef[] { pContext.GetInt(0), pContext.GetInt(0) }, "");
            var variableData = LLVM.BuildInBoundsGEP(pContext.Builder, variable, new LLVMValueRef[] { pContext.GetInt(0), pContext.GetInt(1) }, "");
            LLVM.BuildStore(pContext.Builder, dataAccess, variableData);

            return variable;
        }
    }
}
