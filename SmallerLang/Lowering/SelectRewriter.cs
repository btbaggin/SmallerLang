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
        IfSyntax _currentIf;
        ElseSyntax _currentElse;
        ExpressionSyntax _itVar;
        bool _rewrite;
        protected override SyntaxNode VisitSelectSyntax(SelectSyntax pNode)
        {
            _rewrite = false;
            //Save itvar in case we hit a for statement
            var it = _itVar;
            _itVar = pNode.Condition;
            var s = base.VisitSelectSyntax(pNode);

            if (!_rewrite)
            {
                return s;
            }
            else
            {
                if(Utils.SyntaxHelper.SelectIsComplete(pNode))
                {
                    _error.WriteError("complete annotation is ignored on select statements with 'it'", pNode.Span);
                }

                //Only rewrite if we have "it"
                for (int i = pNode.Cases.Count - 1; i >= 0; i--)
                {
                    var currentCase = pNode.Cases[i];
                    //Default cause needs to be the last one. Make a else statement
                    if(currentCase.IsDefault)
                    {
                        _currentElse = SyntaxFactory.Else(null, currentCase.Body);
                    }
                    else
                    {
                        //The condition needs to be a comparison binary expression
                        ExpressionSyntax e = null;
                        e = (ExpressionSyntax)Visit(currentCase.Conditions[0]);
                        if (e.GetType() != typeof(BinaryExpressionSyntax) ||
                            !IsComparison(((BinaryExpressionSyntax)e).Operator))
                        {
                            //If it isn't make it one
                            e = SyntaxFactory.BinaryExpression(_itVar, BinaryExpressionOperator.Equals, e);
                        }

                        for (int j = 0; j < currentCase.Conditions.Count - 1; j++)
                        {
                            var e1 = currentCase.Conditions[j + 1];
                            if (e1.GetType() != typeof(BinaryExpressionSyntax) || !IsComparison(((BinaryExpressionSyntax)e1).Operator))
                            {
                                //If it isn't make it one
                                e1 = SyntaxFactory.BinaryExpression(_itVar, BinaryExpressionOperator.Equals, e1);
                            }

                            e = SyntaxFactory.BinaryExpression(e, BinaryExpressionOperator.Or, e1);
                        }

                        //Visit body so we can rewrite any "it"
                        var b = (BlockSyntax)Visit(currentCase.Body);
                        _currentIf = SyntaxFactory.If(e, b, _currentElse);

                        if (i > 0)
                        {
                            //As long as this isn't the last statement, make an else if
                            _currentElse = SyntaxFactory.Else(_currentIf, null);
                        }
                    }
                }

                _itVar = it;
                return _currentIf;
            }
        }

        protected override SyntaxNode VisitItSyntax(ItSyntax pNode)
        {
            _rewrite = true;
            return _itVar;
        }

        private bool IsComparison(BinaryExpressionOperator pOp)
        {
            return pOp == BinaryExpressionOperator.Equals ||
                   pOp == BinaryExpressionOperator.GreaterThan ||
                   pOp == BinaryExpressionOperator.GreaterThanOrEqual ||
                   pOp == BinaryExpressionOperator.LessThan ||
                   pOp == BinaryExpressionOperator.LessThanOrEqual ||
                   pOp == BinaryExpressionOperator.NotEquals ||
                   pOp == BinaryExpressionOperator.And ||
                   pOp == BinaryExpressionOperator.Or;
        }
    }
}
