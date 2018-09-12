using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Emitting;
using LLVMSharp;

namespace SmallerLang.Syntax
{
    public enum BinaryExpressionOperator
    {
        Equals,
        NotEquals,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        Addition,
        Subtraction,
        Multiplication,
        Division,
        Mod,
        And,
        Or
    }

    public class BinaryExpressionSyntax : ExpressionSyntax
    {
        public ExpressionSyntax Left { get; private set; }

        public BinaryExpressionOperator Operator { get; private set; }

        public ExpressionSyntax Right { get; private set; }

        private SmallType _type;
        public override SmallType Type
        {
            get { return _type; }
        }

        internal BinaryExpressionSyntax(ExpressionSyntax pLeft, BinaryExpressionOperator pOperator, ExpressionSyntax pRight)
        {
            Left = pLeft;
            Operator = pOperator;
            Right = pRight;
        }

        public override LLVMValueRef Emit(EmittingContext pContext)
        {
            return EmitOperator(Left.Emit(pContext), Operator, Right.Emit(pContext), pContext);
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

        internal static LLVMValueRef EmitOperator(LLVMValueRef pLeft, 
                                                  BinaryExpressionOperator pOp, 
                                                  LLVMValueRef pRight, 
                                                  EmittingContext pContext)
        {
            //We since arrays are pointers, we need to load the value at the pointer
            Utils.LlvmHelper.LoadIfPointer(ref pLeft, pContext);
            Utils.LlvmHelper.LoadIfPointer(ref pRight, pContext);

            bool useFloat = false;
            if(Utils.LlvmHelper.IsFloat(pLeft))
            {
                if (!Utils.LlvmHelper.IsFloat(pRight)) pRight = LLVM.BuildSIToFP(pContext.Builder, pRight, pLeft.TypeOf(), "");
                useFloat = true;
            }
            if(Utils.LlvmHelper.IsFloat(pRight))
            {
                if (!Utils.LlvmHelper.IsFloat(pLeft)) pLeft = LLVM.BuildSIToFP(pContext.Builder, pLeft, pRight.TypeOf(), "");
                useFloat = true;
            }

            switch (pOp)
            {
                case BinaryExpressionOperator.Addition:
                    if (useFloat) return LLVM.BuildFAdd(pContext.Builder, pLeft, pRight, "");
                    else return LLVM.BuildAdd(pContext.Builder, pLeft, pRight, "");

                case BinaryExpressionOperator.Multiplication:
                    if (useFloat) return LLVM.BuildFMul(pContext.Builder, pLeft, pRight, "");
                    else return LLVM.BuildMul(pContext.Builder, pLeft, pRight, "");

                case BinaryExpressionOperator.Subtraction:
                    if (useFloat) return LLVM.BuildFSub(pContext.Builder, pLeft, pRight, "");
                    else return LLVM.BuildSub(pContext.Builder, pLeft, pRight, "");

                case BinaryExpressionOperator.Division:
                    if (useFloat) return LLVM.BuildFDiv(pContext.Builder, pLeft, pRight, "");
                    else return LLVM.BuildSDiv(pContext.Builder, pLeft, pRight, "");

                case BinaryExpressionOperator.Mod:
                    return LLVM.BuildSRem(pContext.Builder, pLeft, pRight, "");

                case BinaryExpressionOperator.And:
                    return LLVM.BuildAnd(pContext.Builder, pLeft, pRight, "");

                case BinaryExpressionOperator.Or:
                    return LLVM.BuildOr(pContext.Builder, pLeft, pRight, "");

                case BinaryExpressionOperator.Equals:
                    if (useFloat) return LLVM.BuildFCmp(pContext.Builder, LLVMRealPredicate.LLVMRealOEQ, pLeft, pRight, "");
                    else return LLVM.BuildICmp(pContext.Builder, LLVMIntPredicate.LLVMIntEQ, pLeft, pRight, "");

                case BinaryExpressionOperator.NotEquals:
                    if (useFloat) return LLVM.BuildFCmp(pContext.Builder, LLVMRealPredicate.LLVMRealONE, pLeft, pRight, "");
                    else return LLVM.BuildICmp(pContext.Builder, LLVMIntPredicate.LLVMIntNE, pLeft, pRight, "");

                case BinaryExpressionOperator.GreaterThan:
                    if (useFloat) return LLVM.BuildFCmp(pContext.Builder, LLVMRealPredicate.LLVMRealOGT, pLeft, pRight, "");
                    else return LLVM.BuildICmp(pContext.Builder, LLVMIntPredicate.LLVMIntSGT, pLeft, pRight, "");

                case BinaryExpressionOperator.GreaterThanOrEqual:
                    if (useFloat) return LLVM.BuildFCmp(pContext.Builder, LLVMRealPredicate.LLVMRealOGE, pLeft, pRight, "");
                    else return LLVM.BuildICmp(pContext.Builder, LLVMIntPredicate.LLVMIntSGE, pLeft, pRight, "");

                case BinaryExpressionOperator.LessThan:
                    if (useFloat) return LLVM.BuildFCmp(pContext.Builder, LLVMRealPredicate.LLVMRealOLT, pLeft, pRight, "");
                    else return LLVM.BuildICmp(pContext.Builder, LLVMIntPredicate.LLVMIntSLT, pLeft, pRight, "");

                case BinaryExpressionOperator.LessThanOrEqual:
                    if (useFloat) return LLVM.BuildFCmp(pContext.Builder, LLVMRealPredicate.LLVMRealOLE, pLeft, pRight, "");
                    else return LLVM.BuildICmp(pContext.Builder, LLVMIntPredicate.LLVMIntSLE, pLeft, pRight, "");

                default:
                    throw new NotImplementedException();
            }
        }

        internal static SmallType GetResultType(SmallType pLeft, BinaryExpressionOperator pOp, SmallType pRight)
        {
            switch(pOp)
            {
                case BinaryExpressionOperator.Addition:
                case BinaryExpressionOperator.Subtraction:
                case BinaryExpressionOperator.Multiplication:
                case BinaryExpressionOperator.Division:
                case BinaryExpressionOperator.Mod:
                    if (pLeft.IsAssignableFrom(pRight)) return pLeft;
                    if (pLeft.IsFloat() && pRight.IsNumber()) return pLeft;
                    if (pRight.IsFloat() && pLeft.IsNumber()) return pRight;
                    return SmallTypeCache.Undefined;

                case BinaryExpressionOperator.Equals:
                case BinaryExpressionOperator.GreaterThan:
                case BinaryExpressionOperator.GreaterThanOrEqual:
                case BinaryExpressionOperator.LessThan:
                case BinaryExpressionOperator.LessThanOrEqual:
                case BinaryExpressionOperator.NotEquals:
                    if (!pLeft.IsAssignableFrom(pRight)) return SmallTypeCache.Undefined;
                    return pLeft;

                default:
                    throw new NotSupportedException("Unknown binary expression operator " + pOp.ToString());
            }
        }
    }
}
