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

            var literal = EscapeString();
            //Save length
            var length = pContext.GetArrayLength(variable);
            LLVM.BuildStore(pContext.Builder, pContext.GetInt(literal.Length), length);

            //Allocate space for our string
            LLVMValueRef dataArray;
            using (var b = new VariableDeclarationBuilder(pContext))
            {
                var charArray = LLVMTypeRef.ArrayType(SmallTypeCache.GetLLVMType(SmallTypeCache.Char, pContext), (uint)(literal.Length + 1)); //Need to allocate 1 more space for the /0
                dataArray = LLVM.BuildAlloca(b.Builder, charArray, ""); 
            }

            //Store the string constant in the allocated array
            var data = pContext.GetString(literal);
            LLVM.BuildStore(pContext.Builder, data, dataArray);

            //Store the allocated array in the string variable
            var dataAccess = LLVM.BuildInBoundsGEP(pContext.Builder, dataArray, new LLVMValueRef[] { pContext.GetInt(0), pContext.GetInt(0) }, "");
            var variableData = LLVM.BuildInBoundsGEP(pContext.Builder, variable, new LLVMValueRef[] { pContext.GetInt(0), pContext.GetInt(1) }, "");
            LLVM.BuildStore(pContext.Builder, dataAccess, variableData);

            return variable;
        }

        private string EscapeString()
        {
            StringBuilder sb = new StringBuilder();
            for(int i= 0; i < Value.Length; i++)
            {
                if(Value[i] == '\\')
                {
                    switch(Value[i + 1])
                    {
                        //These values need to match the values in SmallerLexer
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                    }
                    i++;
                }
                else
                {
                    sb.Append(Value[i]);
                }
            }

            return sb.ToString();
        }
    }
}
