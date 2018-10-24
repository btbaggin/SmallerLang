using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;
using SmallerLang.Emitting;
using System.Diagnostics;

namespace SmallerLang.Lowering
{
    partial class PostTypeRewriter : SyntaxNodeRewriter
    {
        SyntaxNode _itVar;
        SmallType _enumerable;
        public PostTypeRewriter()
        {
            if (NamespaceManager.TryGetStdLib(out NamespaceContainer container))
            {
                _enumerable = container.FindType("Enumerable");
                Debug.Assert(_enumerable != SmallTypeCache.Undefined, "stdlib does not define Enumerable");
#if DEBUG
                Debug.Assert(_enumerable.GetFieldIndex("Count") > -1, "Enumerable does not implement Count");
#endif
            }
            else _enumerable = SmallTypeCache.Undefined;
        }

        protected override SyntaxNode VisitForSyntax(ForSyntax pNode)
        {
            //Rewrite for statements with iterator arrays to normal for statements
            if (pNode.Iterator != null)
            {
                var i = SyntaxFactory.Identifier("!i");

                var postOp = pNode.Reverse ? UnaryExpressionOperator.PostDecrement : UnaryExpressionOperator.PostIncrement;
                UnaryExpressionSyntax finalizer = SyntaxFactory.UnaryExpression(i, postOp);

                SyntaxNode end = null;
                DeclarationSyntax decl = null;

                //Save itvar in case we are looping in a case body
                var it = _itVar;

                if (pNode.Iterator.Type.IsArray)
                {
                    //We are iterating over an array
                    //Reverse loops will start at Array.Length and decrement to 0
                    //Normal loops will start at 0 and increment to Array.Length
                    if (pNode.Reverse)
                    {
                        var length = SyntaxFactory.UnaryExpression(pNode.Iterator, UnaryExpressionOperator.Length);
                        decl = SyntaxFactory.Declaration(new List<IdentifierSyntax>() { i }, SyntaxFactory.BinaryExpression(length, BinaryExpressionOperator.Subtraction, SyntaxFactory.NumericLiteral(1)));
                        end = SyntaxFactory.NumericLiteral(0);
                    }
                    else
                    {
                        decl = SyntaxFactory.Declaration(new List<IdentifierSyntax>() { i }, SyntaxFactory.NumericLiteral(0));
                        end = SyntaxFactory.UnaryExpression(pNode.Iterator, UnaryExpressionOperator.Length);
                    }
                    
                    _itVar = SyntaxFactory.ArrayAccess(pNode.Iterator, i);

                }
                else if (pNode.Iterator.Type.IsAssignableFrom(_enumerable))
                {
                    //We are iterating over an enumerable
                    //Reverse loops will start at Count and decrement to 0
                    //Normal loops will start at 0 and increment to Count
                    if (pNode.Reverse)
                    {
                        var count = SyntaxFactory.MemberAccess(pNode.Iterator, SyntaxFactory.Identifier("Count"));
                        decl = SyntaxFactory.Declaration(new List<IdentifierSyntax>() { i }, SyntaxFactory.BinaryExpression(count, BinaryExpressionOperator.Subtraction, SyntaxFactory.NumericLiteral(1)));
                        end = SyntaxFactory.NumericLiteral(0);
                    }
                    else
                    {
                        decl = SyntaxFactory.Declaration(new List<IdentifierSyntax>() { i }, SyntaxFactory.NumericLiteral(0));
                        end = SyntaxFactory.MemberAccess(pNode.Iterator, SyntaxFactory.Identifier("Count"));
                    }

                    _itVar = SyntaxFactory.MemberAccess(pNode.Iterator, SyntaxFactory.MethodCall("ItemAt", new List<SyntaxNode>() { SyntaxFactory.Identifier("!i") }));
                }
                else
                {
                    _error.WriteError("Iterator variables must be an array or implement Enumerable", pNode.Iterator.Span);
                    return base.VisitForSyntax(pNode);
                }

                var op = pNode.Reverse ? BinaryExpressionOperator.GreaterThanOrEqual : BinaryExpressionOperator.LessThan;
                var condition = SyntaxFactory.BinaryExpression(i, op, end);

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
