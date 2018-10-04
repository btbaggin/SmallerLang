using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;

namespace SmallerLang.Lowering
{
    partial class PostTypeRewriter : SyntaxNodeRewriter
    {
        SyntaxNode _itVar;
        protected override SyntaxNode VisitForSyntax(ForSyntax pNode)
        {
            //Rewrite for statements with iterator arrays to normal for statements
            if (pNode.Iterator != null)
            {
                var i = SyntaxFactory.Identifier("!i");
                var decl = SyntaxFactory.Declaration(new List<IdentifierSyntax>() { i }, SyntaxFactory.NumericLiteral("0", NumberTypes.Integer));
                var finalizer = SyntaxFactory.UnaryExpression(i, UnaryExpressionOperator.PostIncrement);
                SyntaxNode end = null;

                //Save itvar in case we are looping in a case body
                var it = _itVar;

                if (pNode.Iterator.Type.IsArray)
                {
                    end = SyntaxFactory.UnaryExpression(pNode.Iterator, UnaryExpressionOperator.Length);
                    _itVar = SyntaxFactory.ArrayAccess(pNode.Iterator, i);

                }
                else if (pNode.Iterator.Type.IsAssignableFrom(SmallTypeCache.Enumerable))
                {
                    end = SyntaxFactory.MemberAccess(pNode.Iterator, SyntaxFactory.Identifier("Count"));
                    _itVar = SyntaxFactory.MemberAccess(pNode.Iterator, SyntaxFactory.MethodCall("ItemAt", new List<SyntaxNode>() { SyntaxFactory.Identifier("!i") }));
                }
                else
                {
                    _error.WriteError("Iterator variables must be an array or implement Enumerable", pNode.Iterator.Span);
                    return base.VisitForSyntax(pNode);
                }

                var condition = SyntaxFactory.BinaryExpression(i, BinaryExpressionOperator.LessThan, end);

                var body = (BlockSyntax)Visit(pNode.Body);
                _itVar = it;
                return SyntaxFactory.For(new List<DeclarationSyntax>() { decl }, condition, new List<SyntaxNode>() { finalizer }, body);
            }

            return base.VisitForSyntax(pNode);
        }

        protected override SyntaxNode VisitItSyntax(ItSyntax pNode)
        {
            return _itVar ?? pNode;
        }
    }
}
