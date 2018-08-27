using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Emitting;

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

        public override LLVMSharp.LLVMValueRef Emit(EmittingContext pContext)
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

        internal static LLVMSharp.LLVMValueRef EmitOperator(LLVMSharp.LLVMValueRef pLeft, 
                                                            BinaryExpressionOperator pOp, 
                                                            LLVMSharp.LLVMValueRef pRight, 
                                                            EmittingContext pContext)
        {
            //We since arrays are pointers, we need to load the value at the pointer
            Utils.LlvmHelper.LoadIfPointer(ref pLeft, pContext);
            Utils.LlvmHelper.LoadIfPointer(ref pRight, pContext);

            switch (pOp)
            {
                case BinaryExpressionOperator.Addition:
                    return LLVMSharp.LLVM.BuildAdd(pContext.Builder, pLeft, pRight, "");

                case BinaryExpressionOperator.Multiplication:
                    return LLVMSharp.LLVM.BuildMul(pContext.Builder, pLeft, pRight, "");

                case BinaryExpressionOperator.Subtraction:
                    return LLVMSharp.LLVM.BuildSub(pContext.Builder, pLeft, pRight, "");

                case BinaryExpressionOperator.Division:
                    return LLVMSharp.LLVM.BuildSDiv(pContext.Builder, pLeft, pRight, "");

                case BinaryExpressionOperator.Mod:
                    return LLVMSharp.LLVM.BuildSRem(pContext.Builder, pLeft, pRight, "");

                case BinaryExpressionOperator.And:
                    return LLVMSharp.LLVM.BuildAnd(pContext.Builder, pLeft, pRight, "");

                case BinaryExpressionOperator.Or:
                    return LLVMSharp.LLVM.BuildOr(pContext.Builder, pLeft, pRight, "");

                case BinaryExpressionOperator.Equals:
                    if (pLeft.TypeOf().TypeKind == LLVMSharp.LLVMTypeKind.LLVMFloatTypeKind)
                    {
                        return LLVMSharp.LLVM.BuildFCmp(pContext.Builder, LLVMSharp.LLVMRealPredicate.LLVMRealOEQ, pLeft, pRight, "");
                    }
                    else
                    {
                        return LLVMSharp.LLVM.BuildICmp(pContext.Builder, LLVMSharp.LLVMIntPredicate.LLVMIntEQ, pLeft, pRight, "");
                    }

                case BinaryExpressionOperator.NotEquals:
                    if (pLeft.TypeOf().TypeKind == LLVMSharp.LLVMTypeKind.LLVMFloatTypeKind)
                    {
                        return LLVMSharp.LLVM.BuildFCmp(pContext.Builder, LLVMSharp.LLVMRealPredicate.LLVMRealONE, pLeft, pRight, "");
                    }
                    else
                    {
                        return LLVMSharp.LLVM.BuildICmp(pContext.Builder, LLVMSharp.LLVMIntPredicate.LLVMIntNE, pLeft, pRight, "");
                    }

                case BinaryExpressionOperator.GreaterThan:
                    if (pLeft.TypeOf().TypeKind == LLVMSharp.LLVMTypeKind.LLVMFloatTypeKind)
                    {
                        return LLVMSharp.LLVM.BuildFCmp(pContext.Builder, LLVMSharp.LLVMRealPredicate.LLVMRealOGT, pLeft, pRight, "");
                    }
                    else
                    {
                        return LLVMSharp.LLVM.BuildICmp(pContext.Builder, LLVMSharp.LLVMIntPredicate.LLVMIntSGT, pLeft, pRight, "");
                    }

                case BinaryExpressionOperator.GreaterThanOrEqual:
                    if (pLeft.TypeOf().TypeKind == LLVMSharp.LLVMTypeKind.LLVMFloatTypeKind)
                    {
                        return LLVMSharp.LLVM.BuildFCmp(pContext.Builder, LLVMSharp.LLVMRealPredicate.LLVMRealOGE, pLeft, pRight, "");
                    }
                    else
                    {
                        return LLVMSharp.LLVM.BuildICmp(pContext.Builder, LLVMSharp.LLVMIntPredicate.LLVMIntSGE, pLeft, pRight, "");
                    }

                case BinaryExpressionOperator.LessThan:
                    if (pLeft.TypeOf().TypeKind == LLVMSharp.LLVMTypeKind.LLVMFloatTypeKind)
                    {
                        return LLVMSharp.LLVM.BuildFCmp(pContext.Builder, LLVMSharp.LLVMRealPredicate.LLVMRealOLT, pLeft, pRight, "");
                    }
                    else
                    {
                        return LLVMSharp.LLVM.BuildICmp(pContext.Builder, LLVMSharp.LLVMIntPredicate.LLVMIntSLT, pLeft, pRight, "");
                    }

                case BinaryExpressionOperator.LessThanOrEqual:
                    if (pLeft.TypeOf().TypeKind == LLVMSharp.LLVMTypeKind.LLVMFloatTypeKind)
                    {
                        return LLVMSharp.LLVM.BuildFCmp(pContext.Builder, LLVMSharp.LLVMRealPredicate.LLVMRealOLE, pLeft, pRight, "");
                    }
                    else
                    {
                        return LLVMSharp.LLVM.BuildICmp(pContext.Builder, LLVMSharp.LLVMIntPredicate.LLVMIntSLE, pLeft, pRight, "");
                    }

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
