using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using SmallerLang.Emitting;

namespace SmallerLang.Syntax
{
    public class MethodCallSyntax : IdentifierSyntax
    {
        public IList<ExpressionSyntax> Arguments { get; private set; }

        MethodDefinition _definition;

        internal MethodCallSyntax(string pName, IList<ExpressionSyntax> pArguments) : base(pName)
        {
            Arguments = pArguments;
        }

        public override LLVMValueRef Emit(EmittingContext pContext)
        {
            System.Diagnostics.Debug.Assert(_definition.MangledName != null);

            LLVMValueRef[] values = new LLVMValueRef[Arguments.Count];
            for (int i = 0; i < Arguments.Count; i++)
            {
                values[i] = Arguments[i].Emit(pContext);
                var op = values[i].GetInstructionOpcode();

                //For arrays we have the load the pointer reference
                if (!Arguments[i].Type.IsArray && op == LLVMOpcode.LLVMGetElementPtr)
                {
                    values[i] = LLVM.BuildLoad(pContext.Builder, values[i], "argument_" + i.ToString());
                }

                //Implicit any derived types
                if(_definition.ArgumentTypes[i] != Arguments[i].Type)
                {
                    var t = SmallTypeCache.GetLLVMType(_definition.ArgumentTypes[i]);
                    Utils.LlvmHelper.MakePointer(values[i], ref t);
                    values[i] = LLVM.BuildBitCast(pContext.Builder, values[i], t, "");
                }
            }

            return LLVM.BuildCall(pContext.Builder, pContext.GetMethod(_definition.MangledName), values, "");
        }

        internal void SetDefinition(MethodDefinition pDef)
        {
            _definition = pDef;
        }
    }
}
