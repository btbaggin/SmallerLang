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
        internal CastDefinitionSyntax(TypedIdentifierSyntax pFromType, BlockSyntax pBody, TypeSyntax pToType) 
            : base("", new List<TypeSyntax>() { pToType }, new List<TypedIdentifierSyntax>() { pFromType }, pBody, false)
        {
            Name = MethodCache.CAST_METHOD; 
        }
    }
}
