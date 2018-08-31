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

        public IList<ExpressionSyntax> Arguments { get; private set; }

        public override SmallType Type => Struct.Type;

        internal StructInitializerSyntax(string pValue, TypeSyntax pStruct, IList<ExpressionSyntax> pArguments)
        {
            Value = pValue;
            Struct = pStruct;
            Arguments = pArguments;
        }

        public override LLVMValueRef Emit(EmittingContext pContext)
        {
            LLVMValueRef[] arguments = new LLVMValueRef[Arguments.Count + 1];
            arguments[0] = pContext.Locals.GetVariable(Value, out bool p);

            for(int i= 0; i < Arguments.Count; i++)
            {
                arguments[i + 1] = Arguments[i].Emit(pContext);
            }

            //Call constructor for all structs
            var m = Type.GetConstructor();
            LLVM.BuildCall(pContext.Builder, pContext.GetMethod(m.MangledName), arguments, "");
            return default;
        }
    }
}
