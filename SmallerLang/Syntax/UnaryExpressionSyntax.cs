using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Emitting;

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

        public override LLVMSharp.LLVMValueRef Emit(EmittingContext pContext)
        {
            switch(Operator)
            {
                case UnaryExpressionOperator.Not:
                    return LLVMSharp.LLVM.BuildNot(pContext.Builder, Value.Emit(pContext), "");

                case UnaryExpressionOperator.Negative:
                    return LLVMSharp.LLVM.BuildNeg(pContext.Builder, Value.Emit(pContext), "");

                case UnaryExpressionOperator.Length:
                    var l = LLVMSharp.LLVM.BuildInBoundsGEP(pContext.Builder, Value.Emit(pContext), new LLVMSharp.LLVMValueRef[] { pContext.GetInt(0), pContext.GetInt(0) }, "");
                    return LLVMSharp.LLVM.BuildLoad(pContext.Builder, l, "");

                case UnaryExpressionOperator.PreIncrement:
                case UnaryExpressionOperator.PreDecrement:
                case UnaryExpressionOperator.PostIncrement:
                case UnaryExpressionOperator.PostDecrement:
                    var i = (IdentifierSyntax)Value;
                    System.Diagnostics.Debug.Assert(pContext.Locals.IsVariableDefined(i.Value), "Variable " + i.Value + " not defined");

                    var v = pContext.Locals.GetVariable(i.Value);
                    var one = i.Type.IsFloat() ? pContext.GetFloat(1) : pContext.GetInt(1);
                    var iv = i.Emit(pContext);

                    LLVMSharp.LLVMValueRef exp;
                    if (Operator == UnaryExpressionOperator.PreIncrement || Operator == UnaryExpressionOperator.PostIncrement)
                    {
                        exp = i.Type.IsFloat() ? LLVMSharp.LLVM.BuildFAdd(pContext.Builder, iv, one, "") : LLVMSharp.LLVM.BuildAdd(pContext.Builder, iv, one, "");
                    }
                    else
                    {
                        exp = i.Type.IsFloat() ? LLVMSharp.LLVM.BuildFSub(pContext.Builder, iv, one, "") : LLVMSharp.LLVM.BuildSub(pContext.Builder, iv, one, "");
                    }

                    //Pre unary we want to return the original variable value
                    if(Operator == UnaryExpressionOperator.PostIncrement || Operator == UnaryExpressionOperator.PostDecrement)
                    {
                        var temp = LLVMSharp.LLVM.BuildAlloca(pContext.Builder, SmallTypeCache.GetLLVMType(i.Type), "");
                        LLVMSharp.LLVM.BuildStore(pContext.Builder, LLVMSharp.LLVM.BuildLoad(pContext.Builder, v, ""), temp);
                        LLVMSharp.LLVM.BuildStore(pContext.Builder, exp, v);
                        return temp;

                    }

                    LLVMSharp.LLVM.BuildStore(pContext.Builder, exp, v);
                    return v;

                default:
                    throw new NotSupportedException();
            }
        }

        public void SetType(SmallType pType)
        {
            _type = pType;
        }
    }
}
