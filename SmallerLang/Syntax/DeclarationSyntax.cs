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

        public ExpressionSyntax Value { get; private set; }

        public override SmallType Type => SmallTypeCache.Undefined;

        internal DeclarationSyntax(IList<IdentifierSyntax> pVariables, ExpressionSyntax pValue)
        {
            Variables = pVariables;
            Value = pValue;
        }

        public override LLVMValueRef Emit(EmittingContext pContext)
        {
            foreach (var v in Variables)
            {
                //Do not allocate discards
                if(!Utils.SyntaxHelper.IsDiscard(v))
                {
                    System.Diagnostics.Debug.Assert(!pContext.Locals.IsVariableDefinedInScope(v.Value), "Variable " + v.Value + " already defined");

                    LLVMValueRef var = pContext.AllocateVariable(v.Value, v.Type);
                    pContext.Locals.DefineVariableInScope(v.Value, var);
                }
            }

            var value = Value.Emit(pContext);

            if (Variables.Count == 1)
            {
                //We only don't need to assign if we call the constructor since that passes a pointer to the object
                if (Value.GetType() != typeof(StructInitializerSyntax))
                {
                    //Need to load address of pointers
                    Utils.LlvmHelper.LoadIfPointer(ref value, pContext);
                    var v = pContext.Locals.GetVariable(Variables[0].Value);
                    LLVM.BuildStore(pContext.Builder, value, v);
                }

                return value;
            }
            else
            {
                //Create our temp tuple value
                var t = SmallTypeCache.GetOrCreateTuple(Utils.SyntaxHelper.SelectNodeTypes(Variables));

                LLVMValueRef v = pContext.AllocateVariable("<temp>tuple", t);

                //Load the value into our temp variable
                Utils.LlvmHelper.LoadIfPointer(ref value, pContext);
                LLVM.BuildStore(pContext.Builder, value, v);

                //Access each of the tuples fields, assigning them to our variables
                for (int i = 0; i < Variables.Count; i++)
                {
                    if(!Utils.SyntaxHelper.IsDiscard(Variables[i]))
                    {
                        var g = LLVM.BuildInBoundsGEP(pContext.Builder, v, new LLVMValueRef[] { pContext.GetInt(0), pContext.GetInt(i) }, "");
                        g = LLVM.BuildLoad(pContext.Builder, g, "");

                        var variable = pContext.Locals.GetVariable(Variables[i].Value);
                        LLVM.BuildStore(pContext.Builder, g, variable);
                    }
                }

                return value;
            }
        }
    }
}
