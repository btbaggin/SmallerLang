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

            LLVMValueRef[] arguments = null;
            int start = 0;
            //If we are calling an instance method, we need to add the "self" parameter
            if (pContext.MemberAccessStack.Count > 0)
            {
                arguments = new LLVMValueRef[Arguments.Count + 1];
                arguments[0] = pContext.MemberAccessStack.Peek();
                start = 1;
            }
            else
            {
                arguments = new LLVMValueRef[Arguments.Count];
            }
            
            for (int i = 0; i < Arguments.Count; i++)
            {
                arguments[start + i] = Arguments[i].Emit(pContext);

                //Load the location of any pointer calculations
                var op = arguments[start + i].GetInstructionOpcode();
                if (op == LLVMOpcode.LLVMGetElementPtr) arguments[start + i] = LLVM.BuildLoad(pContext.Builder, arguments[start + i], "arg_" + i.ToString());

                //Implicitly cast any derived types
                if(_definition.ArgumentTypes[i] != Arguments[i].Type)
                {
                    var t = SmallTypeCache.GetLLVMType(_definition.ArgumentTypes[i]);
                    Utils.LlvmHelper.MakePointer(arguments[start + i], ref t);
                    arguments[start + i] = LLVM.BuildBitCast(pContext.Builder, arguments[start + i], t, "");
                }
            }

            return LLVM.BuildCall(pContext.Builder, pContext.GetMethod(_definition.MangledName), arguments, "");
        }

        internal void SetDefinition(MethodDefinition pDef)
        {
            _definition = pDef;
        }
    }
}
