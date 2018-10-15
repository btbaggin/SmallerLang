using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using SmallerLang.Emitting;

namespace SmallerLang.Syntax
{
    public class EnumSyntax : SyntaxNode
    {
        public override SmallType Type => SmallTypeCache.Undefined;

        public override SyntaxType SyntaxType => SyntaxType.Enum;

        public string Name { get; private set; }

        public IList<IdentifierSyntax> Names { get; private set; }

        public IList<int> Values { get; private set; }

        internal EnumSyntax(string pName, IList<IdentifierSyntax> pNames, IList<int> pValues)
        {
            Name = pName;
            Names = pNames;
            Values = pValues;
        }

        public override LLVMValueRef Emit(EmittingContext pContext)
        {
            pContext.EmitDebugLocation(this);

            //Enums are emitted by MemberAccessSyntax
            throw new NotImplementedException();
        }
    }
}
