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

    public class UnaryExpressionSyntax : SyntaxNode
    {
        public SyntaxNode Value { get; private set; }

        public UnaryExpressionOperator Operator { get; private set; }

        private SmallType _type;
        public override SmallType Type
        {
            get { return _type; }
        }

        public override SyntaxType SyntaxType => SyntaxType.UnaryExpression;

        internal UnaryExpressionSyntax(SyntaxNode pValue, UnaryExpressionOperator pOperator)
        {
            Value = pValue;
            Operator = pOperator;
        }

        public override LLVMValueRef Emit(EmittingContext pContext)
        {
            pContext.EmitDebugLocation(this);

            LLVMValueRef value;
            switch (Operator)
            {
                case UnaryExpressionOperator.Not:
                    value = Value.Emit(pContext);
                    Utils.LlvmHelper.LoadIfPointer(ref value, pContext);
                    return LLVM.BuildNot(pContext.Builder, value, "");

                case UnaryExpressionOperator.Negative:
                    value = Value.Emit(pContext);
                    Utils.LlvmHelper.LoadIfPointer(ref value, pContext);
                    return LLVM.BuildNeg(pContext.Builder, value, "");

                case UnaryExpressionOperator.Length:
                    var arr = Value.Emit(pContext);
                    return LLVM.BuildLoad(pContext.Builder, pContext.GetArrayLength(arr), "");

                case UnaryExpressionOperator.PreIncrement:
                case UnaryExpressionOperator.PreDecrement:
                case UnaryExpressionOperator.PostIncrement:
                case UnaryExpressionOperator.PostDecrement:
                    var variable = (IdentifierSyntax)Value;
                    variable.DoNotLoad = true;
                    LLVMValueRef v = variable.Emit(pContext);

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

                    value = BinaryExpressionSyntax.EmitOperator(v, op, pContext.GetInt(1), pContext);

                    //Post unary we want to return the original variable value
                    if (Operator == UnaryExpressionOperator.PostIncrement || Operator == UnaryExpressionOperator.PostDecrement)
                    {
                        //Save the old value to a temp variable that we will return
                        var temp = pContext.AllocateVariable("<temp_unary>", Value);
                        LLVMValueRef tempValue = Utils.LlvmHelper.IsPointer(v) ? LLVM.BuildLoad(pContext.Builder, v, "") : v;
                        LLVM.BuildStore(pContext.Builder, tempValue, temp);

                        //Increment the variable
                        Utils.LlvmHelper.LoadIfPointer(ref value, pContext);
                        if (Value.SyntaxType == SyntaxType.Identifier || Value.SyntaxType == SyntaxType.MemberAccess)  LLVM.BuildStore(pContext.Builder, value, v);

                        return temp;
                    }

                    //If it isn't a variable we cane save we need to return the addition
                    if (Value.SyntaxType == SyntaxType.Identifier || Value.SyntaxType == SyntaxType.MemberAccess) LLVM.BuildStore(pContext.Builder, value, v);
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
