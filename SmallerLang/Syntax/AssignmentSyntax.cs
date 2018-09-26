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

    public class AssignmentSyntax : ExpressionSyntax
    {
        public ExpressionSyntax Value { get; private set; }

        public AssignmentOperator Operator { get; private set; }

        public IList<IdentifierSyntax> Variables { get; private set; }

        public override SmallType Type => Value.Type;

        internal AssignmentSyntax(IList<IdentifierSyntax> pVariables, AssignmentOperator pOp, ExpressionSyntax pValue)
        {
            Variables = pVariables;
            Operator = pOp;
            Value = pValue;
        }

        public override LLVMValueRef Emit(EmittingContext pContext)
        {
            pContext.EmitDebugLocation(this);

            LLVMValueRef v;
            if (Variables.Count == 1)
            {
                var variable = Variables[0];
                variable.DoNotLoad = true;

                //we want to make sure to emit the variable before the value 
                //Value could change Variable ie incrementing an array index ie x[y] = y += 1
                //Emit the field if it's a member access
                v = variable.Emit(pContext);
                LLVMValueRef value = Value.Emit(pContext);

                if (Value.GetType() != typeof(StructInitializerSyntax))
                {
                    //Struct initializers take a pointer to the object so we don't need to store it
                    if (Operator != AssignmentOperator.Equals)
                        value = BinaryExpressionSyntax.EmitOperator(v, AssignmentToBinary(Operator), value, pContext);

                    //Load the value of the pointer
                    Utils.LlvmHelper.LoadIfPointer(ref value, pContext);

                    LLVM.BuildStore(pContext.Builder, value, v);
                    return value;
                }
            }
            else
            {
                //Create our temp tuple value
                var t = SmallTypeCache.GetOrCreateTuple(Utils.SyntaxHelper.SelectNodeTypes(Variables));
                v = pContext.AllocateVariable("<temp>tuple", t);

                LLVMValueRef value = Value.Emit(pContext);
                //Load the value into our temp variable
                Utils.LlvmHelper.LoadIfPointer(ref value, pContext);
                LLVM.BuildStore(pContext.Builder, value, v);

                //Access each of the tuples fields, assigning them to our variables
                for (int i = 0; i < Variables.Count; i++)
                {
                    var currentVar = Variables[i];

                    if(!Utils.SyntaxHelper.IsDiscard(currentVar))
                    {
                        currentVar.DoNotLoad = true;
                        var variable = currentVar.Emit(pContext);

                        //Load tuple's field
                        var indexAccess = LLVM.BuildInBoundsGEP(pContext.Builder, v, new LLVMValueRef[] { pContext.GetInt(0), pContext.GetInt(i) }, "");
                        indexAccess = LLVM.BuildLoad(pContext.Builder, indexAccess, "");

                        if (Operator != AssignmentOperator.Equals)
                            indexAccess = BinaryExpressionSyntax.EmitOperator(variable, AssignmentToBinary(Operator), indexAccess, pContext);

                        LLVM.BuildStore(pContext.Builder, indexAccess, variable);
                    }
                }
            }

            return v;
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
