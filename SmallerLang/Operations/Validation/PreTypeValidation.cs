using System;
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
            return pNode.SyntaxType == SyntaxType.BooleanLiteral ||
                   pNode.SyntaxType == SyntaxType.NumericLiteral ||
                   pNode.SyntaxType == SyntaxType.StringLiteral;
        }
    }
}
