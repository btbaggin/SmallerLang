using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;

namespace SmallerLang.Lowering
{
    partial class TreeRewriter : SyntaxNodeRewriter
    {
        HashSet<string> _namespaces;
        protected override SyntaxNode VisitWorkspaceSyntax(WorkspaceSyntax pNode)
        {
            _namespaces = new HashSet<string>();
            foreach(var i in pNode.Imports.Keys)
            {
                _namespaces.Add(i);
            }
            return base.VisitWorkspaceSyntax(pNode);
        }
        protected override SyntaxNode VisitMemberAccessSyntax(MemberAccessSyntax pNode)
        {
            if(_namespaces.Contains(pNode.Identifier.Value))
            {
                return SyntaxFactory.MemberAccess(SyntaxFactory.Namespace(pNode.Identifier.Value), (IdentifierSyntax)Visit(pNode.Value));
            }
            return base.VisitMemberAccessSyntax(pNode);
        }
    }
}
