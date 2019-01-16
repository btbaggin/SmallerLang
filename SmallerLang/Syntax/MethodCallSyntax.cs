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
        public List<SyntaxNode> Arguments { get; private set; }

        public override SyntaxType SyntaxType => SyntaxType.MethodCall;

        MethodDefinition _definition;

        internal MethodCallSyntax(string pName, List<SyntaxNode> pArguments) : base(pName)
        {
            Arguments = pArguments;
        }

        public override LLVMValueRef Emit(EmittingContext pContext)
        {
            System.Diagnostics.Debug.Assert(_definition.MangledName != null);
            pContext.EmitDebugLocation(this);

            LLVMValueRef[] arguments = null;
            int start = 0;

            //If we are calling an instance method, we need to add the "self" parameter
            if (pContext.AccessStack.Count > 0)
            {
                arguments = new LLVMValueRef[Arguments.Count + 1];

                //"consume" the entire access stack to get the object we are calling the method on
                arguments[0] = AccessStack<MemberAccess>.BuildGetElementPtr(pContext, null);

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
                if (!Arguments[i].Type.IsStruct && !Arguments[i].Type.IsArray)
                {
                    Utils.LlvmHelper.LoadIfPointer(ref arguments[start + i], pContext);
                }

                //For external methods passing strings, we only grab the char pointer
                if(_definition.External && Arguments[i].Type == SmallTypeCache.String)
                {
                    if(!Utils.LlvmHelper.IsPointer(arguments[start + i]))
                    {
                        var v = pContext.AllocateVariable(Value + "_temp", Arguments[i].Type);
                        LLVM.BuildStore(pContext.Builder, arguments[start + i], v);
                        arguments[start + i] = v;
                    }
                    arguments[start + i] = LLVM.BuildInBoundsGEP(pContext.Builder, arguments[start + i], new LLVMValueRef[] { pContext.GetInt(0), pContext.GetInt(1) }, "char_pointer");
                    Utils.LlvmHelper.LoadIfPointer(ref arguments[0], pContext);
                }

                //Implicitly cast any derived types
                if (_definition.ArgumentTypes[i] != Arguments[i].Type)
                {
                    //TODO need to handle if _definition comes from a different reference
                    var type = SmallTypeCache.GetLLVMType(_definition.ArgumentTypes[i], pContext);
                    Utils.LlvmHelper.MakePointer(arguments[start + i], ref type);
                    arguments[start + i] = LLVM.BuildBitCast(pContext.Builder, arguments[start + i], type, "");
                }
            }

            return LLVM.BuildCall(pContext.Builder, pContext.GetMethod(_definition.MangledName), arguments, "");
        }

        internal void SetDefinition(in MethodDefinition pDef)
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
