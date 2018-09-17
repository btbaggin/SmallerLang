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

            LLVMTypeRef type = SmallTypeCache.GetLLVMType(Type);
            LLVMValueRef ret;
            if (!string.IsNullOrEmpty(_method.MangledName))
            {
                //User defined cast, call the method
                if(_method.ArgumentTypes[0] != Value.Type)
                {
                    var t = SmallTypeCache.GetLLVMType(_method.ArgumentTypes[0]);
                    Utils.LlvmHelper.MakePointer(val, ref t);
                    val = LLVM.BuildBitCast(pContext.Builder, val, t, "");
                }
                ret = LLVM.BuildCall(pContext.Builder, pContext.GetMethod(_method.MangledName), new LLVMValueRef[] { val }, "user_cast");
            }
            else
            {
                //Built in conversions
                Utils.LlvmHelper.LoadIfPointer(ref val, pContext);
                var fromIsFloat = Utils.TypeHelper.IsFloat(FromType);

                //Implicit cast to boolean, compare value to default
                if (Type == SmallTypeCache.Boolean)
                {
                    var cmp = SmallTypeCache.GetLLVMDefault(FromType, pContext);

                    if (fromIsFloat) ret = LLVM.BuildFCmp(pContext.Builder, LLVMRealPredicate.LLVMRealONE, val, cmp, "");
                    else ret = LLVM.BuildICmp(pContext.Builder, LLVMIntPredicate.LLVMIntNE, val, cmp, "");
                }
                else if (Utils.TypeHelper.IsNumber(FromType) && Utils.TypeHelper.IsFloat(Type))
                {
                    //Int -> Float
                    ret = LLVM.BuildSIToFP(pContext.Builder, val, type, "");
                }
                else if (fromIsFloat && Utils.TypeHelper.IsNumber(Type))
                {
                    //Float -> Int
                    ret = LLVM.BuildFPToSI(pContext.Builder, val, type, "");
                }
                else
                {
                    //Trait cast, it should have been validated that it's already the proper type, just bitcast
                    Utils.LlvmHelper.MakePointer(val, ref type);
                    ret = LLVM.BuildBitCast(pContext.Builder, val, type, "");
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
