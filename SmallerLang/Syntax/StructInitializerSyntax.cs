using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using SmallerLang.Emitting;

namespace SmallerLang.Syntax
{
    public class StructInitializerSyntax : ExpressionSyntax
    {
        public string Value { get; private set; }

        public TypeSyntax Struct { get; private set; }

        public override SmallType Type => Struct.Type;

        internal StructInitializerSyntax(string pValue, TypeSyntax pStruct)
        {
            Value = pValue;
            Struct = pStruct;
        }

        public override LLVMValueRef Emit(EmittingContext pContext)
        {
            var v = pContext.Locals.GetVariable(Value, out bool p);

            //Call constructor for all structs
            LLVM.BuildCall(pContext.Builder, pContext.GetMethod(Type.GetConstructor()), new LLVMValueRef[] { v }, "");
            return default;
        }
    }
}
