//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using LLVMSharp;
//using SmallerLang.Emitting;

//namespace SmallerLang.Syntax
//{
//    public class StructSyntax : TypeDefinitionSyntax
//    {
//        internal StructSyntax(string pName, IList<MethodSyntax> pMethods, IList<TypedIdentifierSyntax> pFields, IList<string> pTypeParms) 
//            : base(pName, pFields, pMethods, pTypeParms) { }

//        public override void EmitMethods(EmittingContext pContext)
//        {
//            var type = SmallTypeCache.FromString(Name);
//            pContext.CurrentStruct = type;
//            foreach(var m in Methods)
//            {
//                m.EmitHeader(pContext);
//            }

//            foreach (var m in Methods)
//            {
//                m.Emit(pContext);
//            }

//            if (!type.HasDefinedConstructor()) EmitGenericConstructor(pContext, type);
//            pContext.CurrentStruct = null;
//        }

//        public override LLVMValueRef Emit(EmittingContext pContext)
//        {
//            pContext.EmitDefinition(Name, this);

//            return default;
//        }

//        private void EmitGenericConstructor(EmittingContext pContext, SmallType pType)
//        {
//            //Emit method header
//            var ret = LLVMTypeRef.VoidType();
//            var parm = new LLVMTypeRef[] { LLVMTypeRef.PointerType(SmallTypeCache.GetLLVMType(pType), 0) };
//            var func = pContext.EmitMethodHeader(Name + ".ctor", ret, parm);

//            var b = LLVM.AppendBasicBlock(func, Name + "body");
//            LLVM.PositionBuilderAtEnd(pContext.Builder, b);

//            LLVMValueRef p = LLVM.GetParam(func, 0);

//            //Emit field assignments
//            for (int i = 0; i < Fields.Count; i++)
//            {
//                LLVMValueRef value = SmallTypeCache.GetLLVMDefault(pType.GetFieldType(Fields[i].Value), pContext);

//                int f = pType.GetFieldIndex(Fields[i].Value);
//                var a = LLVM.BuildInBoundsGEP(pContext.Builder, p, new LLVMValueRef[] { pContext.GetInt(0), pContext.GetInt(f) }, "field_" + Fields[i].Value);

//                LLVM.BuildStore(pContext.Builder, value, a);
//            }

//            LLVM.BuildRetVoid(pContext.Builder);
//        }
//    }
//}
