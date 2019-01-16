using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using SmallerLang.Emitting;

namespace SmallerLang.Syntax
{
    public enum AssignmentOperator
    {
        Equals,
        MultiplyEquals,
        AdditionEquals,
        SubtractionEquals,
        DivisionEquals,
        ConcatEquals,
    }

    public class AssignmentSyntax : SyntaxNode
    {
        public SyntaxNode Value { get; private set; }

        public AssignmentOperator Operator { get; private set; }

        public List<IdentifierSyntax> Variables { get; private set; }

        public override SmallType Type => Value.Type;

        public override SyntaxType SyntaxType => SyntaxType.Assignment;

        internal AssignmentSyntax(List<IdentifierSyntax> pVariables, AssignmentOperator pOp, SyntaxNode pValue)
        {
            Variables = pVariables;
            Operator = pOp;
            Value = pValue;
        }

        public override LLVMValueRef Emit(EmittingContext pContext)
        {
            pContext.EmitDebugLocation(this);
            
            LLVMValueRef value;
            if (Variables.Count == 1)
            {
                var v = Variables[0];
                v.DoNotLoad = true;

                //we want to make sure to emit the variable before the value 
                //Value could change Variable ie incrementing an array index ie x[y] = y += 1
                //Emit the field if it's a member access
                var variable = v.Emit(pContext);
                value = Value.Emit(pContext);

                if (Value.SyntaxType != SyntaxType.StructInitializer)
                {
                    //Struct initializers take a pointer to the object so we don't need to store it
                    if (Operator != AssignmentOperator.Equals)
                        value = BinaryExpressionSyntax.EmitOperator(variable, AssignmentToBinary(Operator), value, pContext);

                    //Load the value of the pointer
                    Utils.LlvmHelper.LoadIfPointer(ref value, pContext);

                    LLVM.BuildStore(pContext.Builder, value, variable);
                }
            }
            else
            {
                value = Value.Emit(pContext);

                if (Value.SyntaxType != SyntaxType.StructInitializer)
                {
                    //Create our temp tuple value
                    var t = SmallTypeCache.GetOrCreateTuple(Utils.SyntaxHelper.SelectNodeTypes(Variables));
                    var tuple = pContext.AllocateVariable("<temp_tuple>", t);
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
                        var currentVar = Variables[i];

                        if (!Utils.SyntaxHelper.IsDiscard(currentVar))
                        {
                            currentVar.DoNotLoad = true;
                            var variable = currentVar.Emit(pContext);

                            //Load tuple's field
                            var indexAccess = LLVM.BuildInBoundsGEP(pContext.Builder, tuple, new LLVMValueRef[] { pContext.GetInt(0), pContext.GetInt(i) }, "");
                            indexAccess = LLVM.BuildLoad(pContext.Builder, indexAccess, "");

                            if (Operator != AssignmentOperator.Equals)
                                indexAccess = BinaryExpressionSyntax.EmitOperator(variable, AssignmentToBinary(Operator), indexAccess, pContext);

                            LLVM.BuildStore(pContext.Builder, indexAccess, variable);
                        }
                    }
                }
            }

            return value;
        }

        private static BinaryExpressionOperator AssignmentToBinary(AssignmentOperator pOp)
        {
            switch (pOp)
            {
                case AssignmentOperator.AdditionEquals:
                    return BinaryExpressionOperator.Addition;

                case AssignmentOperator.ConcatEquals:
                    throw new NotSupportedException();

                case AssignmentOperator.DivisionEquals:
                    return BinaryExpressionOperator.Division;

                case AssignmentOperator.MultiplyEquals:
                    return BinaryExpressionOperator.Multiplication;

                case AssignmentOperator.SubtractionEquals:
                    return BinaryExpressionOperator.Subtraction;

                default:
                    throw new NotSupportedException("Assignment operator does not map to binary operator " + pOp.ToString());
            }
        }
    }
}
