using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;
using SmallerLang.Emitting;
using System.Diagnostics;
using SmallerLang.Utils;

namespace SmallerLang.Operations.Lowering
{
    /*
     * This will take all for blocks that contain interators
     * and transform them into normal for blocks
     * 
     * for(array) stuff...
     * for(let !i = 0: !i < lengthof array: !i++) stuff...
     * 
     * Arrays and enumerables will be generated slightly differently
     * and using ++ or -- will control the loop direction
     */
    partial class PostTypeRewriter : SyntaxNodeRewriter
    {
        SyntaxNode _itVar;
        SmallType _enumerable = null;
        private void GetEnumerable()
        {
            if(SmallTypeCache.TryGetEnumerable(_unit, out _enumerable))
            {
                Debug.Assert(_enumerable.GetFieldIndex("Count") > -1, "Enumerable does not implement Count");
            }
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

                //Declare our iterator outside the for loop
                //This will help if our iterator is complex like a function call
                var iterVar = SyntaxFactory.Identifier("!iter");
                var iterDecl = SyntaxFactory.SingleDeclaration(iterVar, pNode.Iterator);

                if (pNode.Iterator.Type.IsArray)
                {
                    //We are iterating over an array
                    //Reverse loops will start at Array.Length and decrement to 0
                    //Normal loops will start at 0 and increment to Array.Length
                    if (pNode.Reverse)
                    {
                        var length = SyntaxFactory.UnaryExpression(iterVar, UnaryExpressionOperator.Length);
                        decl = SyntaxFactory.SingleDeclaration(i, SyntaxFactory.BinaryExpression(length, BinaryExpressionOperator.Subtraction, SyntaxFactory.NumericLiteral(1)));
                        end = SyntaxFactory.NumericLiteral(0);
                    }
                    else
                    {
                        decl = SyntaxFactory.SingleDeclaration(i, SyntaxFactory.NumericLiteral(0));
                        end = SyntaxFactory.UnaryExpression(iterVar, UnaryExpressionOperator.Length);
                    }
                    
                    _itVar = SyntaxFactory.ArrayAccess(iterVar, i);

                }
                else if (pNode.Iterator.Type.IsAssignableFrom(_enumerable))
                {
                    //We are iterating over an enumerable
                    //Reverse loops will start at Count and decrement to 0
                    //Normal loops will start at 0 and increment to Count
                    if (pNode.Reverse)
                    {
                        var count = SyntaxFactory.MemberAccess(iterVar, SyntaxFactory.Identifier("Count"));
                        decl = SyntaxFactory.SingleDeclaration(i, SyntaxFactory.BinaryExpression(count, BinaryExpressionOperator.Subtraction, SyntaxFactory.NumericLiteral(1)));
                        end = SyntaxFactory.NumericLiteral(0);
                    }
                    else
                    {
                        decl = SyntaxFactory.SingleDeclaration(i, SyntaxFactory.NumericLiteral(0));
                        end = SyntaxFactory.MemberAccess(iterVar, SyntaxFactory.Identifier("Count"));
                    }

                    _itVar = SyntaxFactory.MemberAccess(iterVar, SyntaxFactory.MethodCall("ItemAt", new List<SyntaxNode>() { SyntaxFactory.Identifier("!i") }));
                }
                else
                {
                    //Some bad type. We can't rewrite if it isn't array or enumerable
                    return base.VisitForSyntax(pNode);
                }

                var op = pNode.Reverse ? BinaryExpressionOperator.GreaterThanOrEqual : BinaryExpressionOperator.LessThan;
                var condition = SyntaxFactory.BinaryExpression(i, op, end);

                var body = (BlockSyntax)Visit(pNode.Body);
                var forStatement = SyntaxFactory.For(new List<DeclarationSyntax>() { decl }, condition, new List<SyntaxNode>() { finalizer }, body);

                //Restore our it for any other nested loops
                _itVar = it;

                //Return our iterator declaration and for rewrite
                return SyntaxFactory.Block(new List<SyntaxNode>() { iterDecl, forStatement });
            }

            return base.VisitForSyntax(pNode);
        }

        protected override SyntaxNode VisitItSyntax(ItSyntax pNode)
        {
            return _itVar ?? pNode;
        }
    }
}
