using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;

namespace SmallerLang.Validation
{
    partial class TreeRewriter : SyntaxNodeRewriter
    {
        protected override SyntaxNode VisitForSyntax(ForSyntax pNode)
        {
            //Rewrite for statements with iterator arrays to normal for statements
            if(pNode.Iterator != null)
            {
                var i = SyntaxFactory.Identifier("__i");
                var d = SyntaxFactory.Declaration(new List<IdentifierSyntax>() { i }, SyntaxFactory.NumericLiteral("0", NumberTypes.Integer));
                var c = SyntaxFactory.BinaryExpression(i, BinaryExpressionOperator.LessThan, SyntaxFactory.UnaryExpression(pNode.Iterator, UnaryExpressionOperator.Length));
                var f = SyntaxFactory.UnaryExpression(i, UnaryExpressionOperator.PostIncrement);

                //Save itvar in case we are looping in a case body
                var it = _itVar;
                _itVar = SyntaxFactory.ArrayAccess(pNode.Iterator.Value, i);
                var b = (BlockSyntax)Visit(pNode.Body);
                _itVar = it;

                return SyntaxFactory.For(new List<DeclarationSyntax>() { d }, c, new List<ExpressionSyntax>() { f }, b);
            }

            return base.VisitForSyntax(pNode);
        }
    }
}
