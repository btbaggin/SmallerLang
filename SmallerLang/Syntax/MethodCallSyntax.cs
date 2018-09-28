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
        public IList<SyntaxNode> Arguments { get; private set; }

        public override SyntaxType SyntaxType => SyntaxType.MethodCall;

        MethodDefinition _definition;

        internal MethodCallSyntax(string pName, IList<SyntaxNode> pArguments) : base(pName)
        {
            Arguments = pArguments;
        }

        public override LLVMValueRef Emit(EmittingContext pContext)
        {
            System.Diagnostics.Debug.Assert(_definition.MangledName != null);

            LLVMValueRef[] arguments = null;
            MemberAccessStack member = null;
            int start = 0;
            //If we are calling an instance method, we need to add the "self" parameter
            if (pContext.AccessStack.Count > 0)
            {
                arguments = new LLVMValueRef[Arguments.Count + 1];

                //Save the current stack so we can restore it when we are done
                //We clear this because we are no longer in a member access when emitting the arguments
                member = pContext.AccessStack.Copy();

                //"consume" the entire access stack to get the object we are calling the method on
                arguments[0] = MemberAccessStack.BuildGetElementPtr(pContext, null);

                pContext.AccessStack.Clear();
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
                //The exceptions to this are structs (arrays are structs) since we pass those as a pointer
                if (Utils.LlvmHelper.IsPointer(arguments[start + i]) && !Arguments[i].Type.IsStruct && !Arguments[i].Type.IsArray)
                {
                    arguments[start + i] = LLVM.BuildLoad(pContext.Builder, arguments[start + i], "arg_" + i.ToString());
                }

                //Implicitly cast any derived types
                if (_definition.ArgumentTypes[i] != Arguments[i].Type)
                {
                    var type = SmallTypeCache.GetLLVMType(_definition.ArgumentTypes[i]);
                    Utils.LlvmHelper.MakePointer(arguments[start + i], ref type);
                    arguments[start + i] = LLVM.BuildBitCast(pContext.Builder, arguments[start + i], type, "");
                }
            }

            if (member != null) pContext.AccessStack = member;

            return LLVM.BuildCall(pContext.Builder, pContext.GetMethod(_definition.MangledName), arguments, "");
        }

        internal void SetDefinition(MethodDefinition pDef)
        {
            _definition = pDef;
        }

        public override T FromNode<T>(T pNode)
        {
            _definition = (pNode as MethodCallSyntax)._definition;
            return base.FromNode(pNode);
        }
    }
}
