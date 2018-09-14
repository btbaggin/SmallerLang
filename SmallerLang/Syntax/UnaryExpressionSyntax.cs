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
            switch(Operator)
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
                    var v = Value.Emit(pContext);
                    Utils.LlvmHelper.LoadIfPointer(ref v, pContext);
                    var iIsFloat = Utils.LlvmHelper.IsFloat(v);

                    LLVMValueRef one;
                    if (v.TypeOf().TypeKind == LLVMTypeKind.LLVMFloatTypeKind) one = pContext.GetFloat(1);
                    else if (v.TypeOf().TypeKind == LLVMTypeKind.LLVMDoubleTypeKind) one = pContext.GetDouble(1);
                    else one = pContext.GetInt(1);

                    LLVMValueRef exp;
                    if (Operator == UnaryExpressionOperator.PreIncrement || Operator == UnaryExpressionOperator.PostIncrement)
                    {
                        exp = iIsFloat ? LLVM.BuildFAdd(pContext.Builder, v, one, "") : LLVM.BuildAdd(pContext.Builder, v, one, "");
                    }
                    else
                    {
                        exp = iIsFloat ? LLVM.BuildFSub(pContext.Builder, v, one, "") : LLVM.BuildSub(pContext.Builder, v, one, "");
                    }

                    //Post unary we want to return the original variable value
                    if(Operator == UnaryExpressionOperator.PostIncrement || Operator == UnaryExpressionOperator.PostDecrement)
                    {
                        var temp = pContext.AllocateVariable("<temp_unary>", Value.Type);
                        LLVM.BuildStore(pContext.Builder, v, temp);

                        StoreValueIfVariable(v, exp, pContext);
                        return temp;
                    }
                    
                    //If it isn't a variable we cane save we need to return the addition
                    if (StoreValueIfVariable(v, exp, pContext)) return v;
                    return exp;

                default:
                    throw new NotSupportedException();
            }
        }

        public void SetType(SmallType pType)
        {
            _type = pType;
        }

        public override SyntaxNode FromNode(SyntaxNode pNode)
        {
            _type = pNode.Type;
            return base.FromNode(pNode);
        }

        private bool StoreValueIfVariable(LLVMValueRef pVariable, LLVMValueRef pValue, EmittingContext pContext)
        {
            //We only want to store the value back if it's an actual variable.
            //If it's a function call or something else we can't save it
            var v = Value;
            if (Value is MemberAccessSyntax m) v = m.Value;
            if (v is IdentifierSyntax i)
            {
                pVariable = pContext.Locals.GetVariable(i.Value, out bool parameter);
                LLVM.BuildStore(pContext.Builder, pValue, pVariable);
                return true;
            }

            return false;
        }
    }
}
