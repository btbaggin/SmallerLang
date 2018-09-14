using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using SmallerLang.Emitting;

namespace SmallerLang.Syntax
{
    public class CastSyntax : UnaryExpressionSyntax
    {
        public SmallType FromType => Value.Type;

        public override SmallType Type
        {
            get
            {
                if (TypeNode == null) return base.Type;
                return TypeNode.Type;
            }
        }

        public TypeSyntax TypeNode { get; private set; }

        MethodDefinition _method;
        internal CastSyntax(ExpressionSyntax pValue) : this(pValue, null) { }

        internal CastSyntax(ExpressionSyntax pValue, TypeSyntax pType) : base(pValue, UnaryExpressionOperator.Cast)
        {
            TypeNode = pType;
            if (TypeNode == null) SetType(SmallTypeCache.Undefined);
        }

        public override LLVMValueRef Emit(EmittingContext pContext)
        {
            var val = Value.Emit(pContext);
            if (FromType == Type) return val;

            LLVMTypeRef t = SmallTypeCache.GetLLVMType(Type);
            LLVMValueRef ret;
            if (!string.IsNullOrEmpty(_method.MangledName))
            {
                if(_method.ArgumentTypes[0] != Value.Type)
                {
                    var type = SmallTypeCache.GetLLVMType(_method.ArgumentTypes[0]);
                    Utils.LlvmHelper.MakePointer(val, ref type);
                    val = LLVM.BuildBitCast(pContext.Builder, val, type, "bitcast");
                }
                ret = LLVM.BuildCall(pContext.Builder, pContext.GetMethod(_method.MangledName), new LLVMValueRef[] { val }, "");
            }
            else
            {
                Utils.LlvmHelper.LoadIfPointer(ref val, pContext);

                if (Type == SmallTypeCache.Boolean)
                {
                    var cmp = SmallTypeCache.GetLLVMDefault(FromType, pContext);

                    if (Utils.TypeHelper.IsFloat(FromType))
                    {
                        ret = LLVM.BuildFCmp(pContext.Builder, LLVMRealPredicate.LLVMRealONE, val, cmp, "cast");
                    }
                    else
                    {
                        ret = LLVM.BuildICmp(pContext.Builder, LLVMIntPredicate.LLVMIntNE, val, cmp, "cast");
                    }
                }
                else if (Utils.TypeHelper.IsNumber(FromType) && Utils.TypeHelper.IsFloat(Type))
                {
                    ret = LLVM.BuildSIToFP(pContext.Builder, val, t, "");
                }
                else if (Utils.TypeHelper.IsFloat(FromType) && Utils.TypeHelper.IsNumber(Type))
                {
                    ret = LLVM.BuildFPToSI(pContext.Builder, val, t, "");
                }
                else
                {
                    Utils.LlvmHelper.MakePointer(val, ref t);
                    ret = LLVM.BuildBitCast(pContext.Builder, val, t, "bitcast");
                }
            }

            return ret;
        }

        internal void SetMethod(MethodDefinition pMethod)
        {
            _method = pMethod;
        }
    }
}
