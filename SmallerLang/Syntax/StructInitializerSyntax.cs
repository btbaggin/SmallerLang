using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using SmallerLang.Emitting;

namespace SmallerLang.Syntax
{
    public class StructInitializerSyntax : SyntaxNode
    {
        public IList<IdentifierSyntax> Values { get; private set; }

        public TypeSyntax Struct { get; private set; }

        public IList<SyntaxNode> Arguments { get; private set; }

        public override SmallType Type => Struct.Type;

        public override SyntaxType SyntaxType => SyntaxType.StructInitializer;

        internal StructInitializerSyntax(IList<IdentifierSyntax> pValue, TypeSyntax pStruct, IList<SyntaxNode> pArguments)
        {
            Values = pValue;
            Struct = pStruct;
            Arguments = pArguments;
        }

        public override LLVMValueRef Emit(EmittingContext pContext)
        {
            pContext.EmitDebugLocation(this);

            LLVMValueRef[] arguments = new LLVMValueRef[Arguments.Count + 1];
            var m = Type.GetConstructor();

            foreach (var v in Values)
            {
                v.DoNotLoad = true;
                arguments[0] = v.Emit(pContext);

                for (int i = 0; i < Arguments.Count; i++)
                {
                    arguments[i + 1] = Arguments[i].Emit(pContext);

                    //Load the location of any pointer calculations
                    var op = arguments[i + 1].GetInstructionOpcode();
                    if (op == LLVMOpcode.LLVMGetElementPtr) arguments[i + 1] = LLVM.BuildLoad(pContext.Builder, arguments[i + 1], "arg_" + i.ToString());

                    //Implicitly cast any derived types
                    if (m.ArgumentTypes[i] != Arguments[i].Type)
                    {
                        var t = SmallTypeCache.GetLLVMType(m.ArgumentTypes[i], pContext);
                        Utils.LlvmHelper.MakePointer(arguments[i + 1], ref t);
                        arguments[i + 1] = LLVM.BuildBitCast(pContext.Builder, arguments[i + 1], t, "");
                    }
                }

                //Call constructor for all structs
                LLVM.BuildCall(pContext.Builder, pContext.GetMethod(m.MangledName), arguments, "");
            }

            return default;
        }
    }
}
