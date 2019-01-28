using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;
using SmallerLang.Emitting;

namespace SmallerLang.Operations.Lowering
{
    partial class PreTypeRewriter : SyntaxNodeRewriter
    {
        protected override SyntaxNode VisitMemberAccessSyntax(MemberAccessSyntax pNode)
        {
            if(_unit.HasReference(pNode.Identifier.Value))
            {
                return SyntaxFactory.MemberAccess(SyntaxFactory.Namespace(pNode.Identifier.Value), (IdentifierSyntax)Visit(pNode.Value));
            }
            return base.VisitMemberAccessSyntax(pNode);
        }

        protected override SyntaxNode VisitNamespaceSyntax(NamespaceSyntax pNode)
        {
            if (!_unit.HasReference(pNode.Value))
            {
                Utils.CompilerErrors.NamespaceNotDefined(pNode, pNode.Span);
            }
            return base.VisitNamespaceSyntax(pNode);
        }
    }
}
