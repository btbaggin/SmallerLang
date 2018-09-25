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

        public uint Size
        {
            get
            {
                uint.TryParse(Value, out uint i);
                return i;
            }
        }

        public ArrayLiteralSyntax(TypeSyntax pType, string pValue) : base(pValue)
        {
            TypeNode = pType; 
        }

        public override LLVMValueRef Emit(EmittingContext pContext)
        {
            var variable = pContext.AllocateVariable("array_temp", Type);

            var length = LLVM.BuildInBoundsGEP(pContext.Builder, variable, new LLVMValueRef[] { pContext.GetInt(0), pContext.GetInt(0) }, "");
            LLVM.BuildStore(pContext.Builder, pContext.GetInt(int.Parse(Value)), length);

            var data = LLVM.BuildAlloca(pContext.Builder, LLVMTypeRef.ArrayType(SmallTypeCache.GetLLVMType(Type.GetElementType()), Size), "");
            var dataAccess = LLVM.BuildInBoundsGEP(pContext.Builder, data, new LLVMValueRef[] { pContext.GetInt(0), pContext.GetInt(0) }, "");
            var variableData = LLVM.BuildInBoundsGEP(pContext.Builder, variable, new LLVMValueRef[] { pContext.GetInt(0), pContext.GetInt(1) }, "");
            LLVM.BuildStore(pContext.Builder, dataAccess, variableData);

            return variable;
        }
    }
}
