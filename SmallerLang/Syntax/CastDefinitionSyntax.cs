using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using SmallerLang.Emitting;

namespace SmallerLang.Syntax
{
    public class CastDefinitionSyntax : MethodSyntax
    {
        public override SyntaxType SyntaxType => SyntaxType.CastDefinition;

        internal CastDefinitionSyntax(FileScope pScope, TypedIdentifierSyntax pFromType, BlockSyntax pBody, TypeSyntax pToType) 
            : base(pScope, "", new List<TypeSyntax>() { pToType }, new List<TypedIdentifierSyntax>() { pFromType }, pBody, false)
        {
            Name = MethodCache.CAST_METHOD; 
        }
    }
}
