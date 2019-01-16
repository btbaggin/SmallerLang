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

        public List<IdentifierSyntax> Names { get; private set; }

        public List<int> Values { get; private set; }

        public FileScope Scope { get; private set; }

        internal EnumSyntax(FileScope pScope, string pName, List<IdentifierSyntax> pNames, List<int> pValues)
        {
            Scope = pScope;
            Name = pName;
            Names = pNames;
            Values = pValues;
        }

        public override LLVMValueRef Emit(EmittingContext pContext)
        {
            //Enums are emitted by MemberAccessSyntax
            throw new NotImplementedException();
        }
    }
}
