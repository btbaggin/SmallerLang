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

        public IList<ExpressionSyntax> Variables { get; private set; }

        public override SmallType Type => Value.Type;

        internal AssignmentSyntax(IList<ExpressionSyntax> pVariables, AssignmentOperator pOp, ExpressionSyntax pValue)
        {
            Variables = pVariables;
            Operator = pOp;
            Value = pValue;
        }

        public override LLVMValueRef Emit(EmittingContext pContext)
        {
#if DEBUG
            foreach (var v in Variables)
            {
                //It's safe to cast because the types have been validated already
                var variable = (IdentifierSyntax)v;

                //Do not allocate discards
                if (!Utils.SyntaxHelper.IsDiscard(v))
                {
                    System.Diagnostics.Debug.Assert(pContext.Locals.IsVariableDefined(variable.Value), $"Variable {variable.Value} not defined");
                }
            }
#endif

            if (Variables.Count == 1)
            {
                var variable = (IdentifierSyntax)Variables[0];
                //we want to make sure to emit the variable before the index 
                //Value could change Variable ie incrementing an array index ie x[y] = y += 1
                //Emit the field if it's a member access
                LLVMValueRef v = pContext.Locals.GetVariable(variable.Value);

                if (variable.IsMemberAccess) v = variable.Emit(pContext);
                LLVMValueRef value = Value.Emit(pContext);

                if (Operator != AssignmentOperator.Equals)
                    value = BinaryExpressionSyntax.EmitOperator(v, AssignToBin(Operator), value, pContext);

                //Load the value of the pointer
                Utils.LlvmHelper.LoadIfPointer(ref value, pContext);

                //Struct initializers take a pointer to the object so we don't need to store it
                if (Value.GetType() != typeof(StructInitializerSyntax))
                {
                    LLVM.BuildStore(pContext.Builder, value, v);
                    return value;
                }

                return v;
            }
            else
            {
                //Create our temp tuple value
                var t = SmallTypeCache.GetOrCreateTuple(Utils.SyntaxHelper.SelectNodeTypes(Variables));
                LLVMValueRef v = pContext.AllocateVariable("<temp>tuple", t);

                LLVMValueRef value = Value.Emit(pContext);
                //Load the value into our temp variable
                Utils.LlvmHelper.LoadIfPointer(ref value, pContext);
                LLVM.BuildStore(pContext.Builder, value, v);

                //Access each of the tuples fields, assigning them to our variables
                for (int i = 0; i < Variables.Count; i++)
                {
                    var currentVar = (IdentifierSyntax)Variables[i];
                    if(!Utils.SyntaxHelper.IsDiscard(currentVar))
                    {
                        var variable = pContext.Locals.GetVariable(currentVar.Value);

                        //Emit the field if it's a member access
                        if (currentVar.IsMemberAccess) v = currentVar.Emit(pContext);

                        //Load tuple's field
                        var g = LLVM.BuildInBoundsGEP(pContext.Builder, v, new LLVMValueRef[] { pContext.GetInt(0), pContext.GetInt(i) }, "");
                        g = LLVM.BuildLoad(pContext.Builder, g, "");

                        if (Operator != AssignmentOperator.Equals)
                            g = BinaryExpressionSyntax.EmitOperator(variable, AssignToBin(Operator), g, pContext);

                        LLVM.BuildStore(pContext.Builder, g, variable);
                    }
                }

                return v;
            }
        }

        private static BinaryExpressionOperator AssignToBin(AssignmentOperator pOp)
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
