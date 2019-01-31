﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;
using SmallerLang.Utils;

namespace SmallerLang.Operations.Validation
{
    class PreTypeValidation : SyntaxNodeVisitor
    {
        readonly Compiler.CompilationCache _cache;
        public PreTypeValidation(Compiler.CompilationCache pCache)
        {
            _cache = pCache;
        }

        protected override void VisitTypeDefinitionSyntax(TypeDefinitionSyntax pNode)
        {
            if(pNode.DefinitionType != DefinitionTypes.Implement && pNode.DeclaredType.Namespace != null)
            {
                 CompilerErrors.StructNamespace(pNode.Span);
            }
            base.VisitTypeDefinitionSyntax(pNode);
        }

        protected override void VisitDeclarationSyntax(DeclarationSyntax pNode)
        {
            if(pNode.IsConst && !IsConstant(pNode.Value))
            {
                CompilerErrors.ConstantNotConstantValue(pNode.Value.Span);
            }
            base.VisitDeclarationSyntax(pNode);
        }

        private bool IsConstant(SyntaxNode pNode)
        {
            if(pNode.SyntaxType == SyntaxType.BooleanLiteral ||
               pNode.SyntaxType == SyntaxType.NumericLiteral ||
               pNode.SyntaxType == SyntaxType.StringLiteral)
            {
                return true;
            }
            else if(pNode.SyntaxType == SyntaxType.MemberAccess)
            {
                var ma = (MemberAccessSyntax)pNode;
                if(_cache.IsTypeDefined(ma.Identifier.Value) && _cache.FromString(ma.Identifier.Value, out SmallType t) == Compiler.FindResult.Found)
                {
                    return t.IsEnum;
                }
            }

            return false;
        }
    }
}
