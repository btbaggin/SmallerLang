using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using SmallerLang.Emitting;

namespace SmallerLang.Syntax
{
    public class ArrayLiteralSyntax : IdentifierSyntax
    {
        public TypeSyntax TypeNode { get; private set; }

        public override SmallType Type
        {
            get { return TypeNode.Type; }
        }

        public override SyntaxType SyntaxType => SyntaxType.ArrayLiteral;

        public SyntaxNode Size { get; private set; }

        internal ArrayLiteralSyntax(TypeSyntax pType, SyntaxNode pSize) : base("")
        {
            TypeNode = pType;
            Size = pSize;
        }

        public override LLVMValueRef Emit(EmittingContext pContext)
        {
            pContext.EmitDebugLocation(this);

            var variable = pContext.AllocateVariable("array_temp", this);

            var size = Size.Emit(pContext);
            var length = LLVM.BuildInBoundsGEP(pContext.Builder, variable, new LLVMValueRef[] { pContext.GetInt(0), pContext.GetInt(0) }, "");
            LLVM.BuildStore(pContext.Builder, size, length);

            var data = pContext.AllocateArrayLiteral(Type.GetElementType(), size);
            var variableData = LLVM.BuildInBoundsGEP(pContext.Builder, variable, new LLVMValueRef[] { pContext.GetInt(0), pContext.GetInt(1) }, "");
            LLVM.BuildStore(pContext.Builder, data, variableData);

            return variable;
        }
    }
}
