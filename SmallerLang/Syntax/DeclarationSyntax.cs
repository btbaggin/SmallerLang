using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Emitting;
using LLVMSharp;

namespace SmallerLang.Syntax
{
    public class DeclarationSyntax : SyntaxNode
    {
        public IList<IdentifierSyntax> Variables { get; private set; }

        public SyntaxNode Value { get; private set; }

        public override SmallType Type => SmallTypeCache.Undefined;

        public override SyntaxType SyntaxType => SyntaxType.Declaration;

        internal DeclarationSyntax(IList<IdentifierSyntax> pVariables, SyntaxNode pValue)
        {
            Variables = pVariables;
            Value = pValue;
        }

        public override LLVMValueRef Emit(EmittingContext pContext)
        {
            pContext.EmitDebugLocation(this);

            foreach (var v in Variables)
            {
                //Do not allocate discards
                if(!Utils.SyntaxHelper.IsDiscard(v))
                {
                    System.Diagnostics.Debug.Assert(!pContext.Locals.IsVariableDefinedInScope(v.Value), "Variable " + v.Value + " already defined");

                    LLVMValueRef var = pContext.AllocateVariable(v.Value, v.Type);
                    pContext.Locals.DefineVariableInScope(v.Value, v.Type, var);
                }
            }

            var value = Value.Emit(pContext);

            //We only don't need to assign if we call the constructor since that passes a pointer to the object
            if (Value.SyntaxType != SyntaxType.StructInitializer)
            {
                if (Variables.Count == 1)
                {
                    //Need to load address of pointers
                    Utils.LlvmHelper.LoadIfPointer(ref value, pContext);
                    var v = pContext.Locals.GetVariable(Variables[0].Value);
                    LLVM.BuildStore(pContext.Builder, value, v.Value);
                }
                else
                {
                    //Create our temp tuple value
                    var t = SmallTypeCache.GetOrCreateTuple(Utils.SyntaxHelper.SelectNodeTypes(Variables));
                    LLVMValueRef tuple = pContext.AllocateVariable("<temp_tuple>", t);
                    Utils.LlvmHelper.LoadIfPointer(ref value, pContext);

                    //If our value is not a tuple we need to allocate the tuple fields manually
                    if (!Value.Type.IsTuple)
                    {
                        for (int i = 0; i < t.GetFieldCount(); i++)
                        {
                            var tv = LLVM.BuildInBoundsGEP(pContext.Builder, tuple, new LLVMValueRef[] { pContext.GetInt(0), pContext.GetInt(i) }, "");
                            LLVM.BuildStore(pContext.Builder, value, tv);

                            //Certain things must be reallocated every time 
                            //otherwise value would just be pointing at the same memory location, causing assignments to affect multiple variables
                            if (Utils.SyntaxHelper.MustReallocateOnDeclaration(Value))
                            {
                                value = Value.Emit(pContext);
                                Utils.LlvmHelper.LoadIfPointer(ref value, pContext);
                            }
                        }
                    }
                    else
                    {
                        //Load the value into our temp variable
                        LLVM.BuildStore(pContext.Builder, value, tuple);
                    }            

                    //Access each of the tuples fields, assigning them to our variables
                    for (int i = 0; i < Variables.Count; i++)
                    {
                        if (!Utils.SyntaxHelper.IsDiscard(Variables[i]))
                        {
                            var indexAccess = LLVM.BuildInBoundsGEP(pContext.Builder, tuple, new LLVMValueRef[] { pContext.GetInt(0), pContext.GetInt(i) }, "");
                            indexAccess = LLVM.BuildLoad(pContext.Builder, indexAccess, "");

                            var variable = pContext.Locals.GetVariable(Variables[i].Value);
                            LLVM.BuildStore(pContext.Builder, indexAccess, variable.Value);
                        }
                    }
                }

            }
            return value;
        }
    }
}
