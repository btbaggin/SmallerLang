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
        protected override SyntaxNode VisitWorkspaceSyntax(WorkspaceSyntax pNode)
        {
            foreach(var i in pNode.Imports.Values)
            {
                NamespaceManager.AddNamespace(i.LibraryPath, i.Namespace);
            }
            NamespaceManager.AddNamespace("", "");

            return base.VisitWorkspaceSyntax(pNode);
        }

        protected override SyntaxNode VisitMemberAccessSyntax(MemberAccessSyntax pNode)
        {
            if(NamespaceManager.HasNamespace(pNode.Identifier.Value))
            {
                return SyntaxFactory.MemberAccess(SyntaxFactory.Namespace(pNode.Identifier.Value), (IdentifierSyntax)Visit(pNode.Value));
            }
            return base.VisitMemberAccessSyntax(pNode);
        }
    }
}
