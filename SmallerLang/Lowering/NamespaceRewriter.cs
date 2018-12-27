using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;
using SmallerLang.Emitting;

namespace SmallerLang.Lowering
{
    partial class TreeRewriter : SyntaxNodeRewriter
    {
        protected override SyntaxNode VisitMemberAccessSyntax(MemberAccessSyntax pNode)
        {
            if(_unit.HasReference(pNode.Identifier.Value))
            {
                return SyntaxFactory.MemberAccess(SyntaxFactory.Namespace(pNode.Identifier.Value), (IdentifierSyntax)Visit(pNode.Value));
            }
            return base.VisitMemberAccessSyntax(pNode);
        }
    }
}
