using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Emitting;
using LLVMSharp;

namespace SmallerLang.Syntax
{
    public enum UnaryExpressionOperator
    {
        Length,
        Cast,
        Not,
        Negative,
        PreIncrement,
        PreDecrement,
        PostIncrement,
        PostDecrement,
    }

    public class UnaryExpressionSyntax : ExpressionSyntax
    {
        public ExpressionSyntax Value { get; private set; }

        public UnaryExpressionOperator Operator { get; private set; }

        private SmallType _type;
        public override SmallType Type
        {
            get { return _type; }
        }

        internal UnaryExpressionSyntax(ExpressionSyntax pValue, UnaryExpressionOperator pOperator)
        {
            Value = pValue;
            Operator = pOperator;
        }

        public override LLVMValueRef Emit(EmittingContext pContext)
        {
            pContext.EmitDebugLocation(this);

            switch (Operator)
            {
                case UnaryExpressionOperator.Not:
                    return LLVM.BuildNot(pContext.Builder, Value.Emit(pContext), "");

                case UnaryExpressionOperator.Negative:
                    return LLVM.BuildNeg(pContext.Builder, Value.Emit(pContext), "");

                case UnaryExpressionOperator.Length:
                    var l = LLVM.BuildInBoundsGEP(pContext.Builder, Value.Emit(pContext), new LLVMValueRef[] { pContext.GetInt(0), pContext.GetInt(0) }, "");
                    return LLVM.BuildLoad(pContext.Builder, l, "");

                case UnaryExpressionOperator.PreIncrement:
                case UnaryExpressionOperator.PreDecrement:
                case UnaryExpressionOperator.PostIncrement:
                case UnaryExpressionOperator.PostDecrement:
                    var variable = (IdentifierSyntax)Value;
                    LLVMValueRef v = pContext.Locals.GetVariable(variable.Value);

                    if (variable.IsMemberAccess) v = variable.Emit(pContext);

                    BinaryExpressionOperator op = BinaryExpressionOperator.Equals;
                    switch (Operator)
                    {
                        case UnaryExpressionOperator.PostDecrement:
                        case UnaryExpressionOperator.PreDecrement:
                            op = BinaryExpressionOperator.Subtraction;
                            break;

                        case UnaryExpressionOperator.PostIncrement:
                        case UnaryExpressionOperator.PreIncrement:
                            op = BinaryExpressionOperator.Addition;
                            break;
                    }

                    LLVMValueRef value = BinaryExpressionSyntax.EmitOperator(v, op, pContext.GetInt(1), pContext);

                    //Post unary we want to return the original variable value
                    if (Operator == UnaryExpressionOperator.PostIncrement || Operator == UnaryExpressionOperator.PostDecrement)
                    {
                        //Save the old value to a temp variable that we will return
                        var temp = pContext.AllocateVariable("<temp_unary>", Value);
                        LLVMValueRef tempValue = Utils.LlvmHelper.IsPointer(v) ? LLVM.BuildLoad(pContext.Builder, v, "") : v;
                        LLVM.BuildStore(pContext.Builder, tempValue, temp);

                        //Increment the variable
                        Utils.LlvmHelper.LoadIfPointer(ref value, pContext);
                        if (Value.GetType() == typeof(IdentifierSyntax) || Value.GetType() == typeof(MemberAccessSyntax))  LLVM.BuildStore(pContext.Builder, value, v);

                        return temp;
                    }

                    //If it isn't a variable we cane save we need to return the addition
                    if (Value.GetType() == typeof(IdentifierSyntax) || Value.GetType() == typeof(MemberAccessSyntax)) LLVM.BuildStore(pContext.Builder, value, v);
                    return value;

                default:
                    throw new NotSupportedException();
            }
        }

        public void SetType(SmallType pType)
        {
            _type = pType;
        }

        public override T FromNode<T>(T pNode)
        {
            _type = pNode.Type;
            return base.FromNode(pNode);
        }
    }
}
