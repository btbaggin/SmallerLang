﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;
using SmallerLang.Utils;

namespace SmallerLang.Operations.Lowering
{
    partial class PostTypeRewriter : SyntaxNodeRewriter
    {
        IfSyntax _currentIf;
        ElseSyntax _currentElse;
        bool _rewrite;
        protected override SyntaxNode VisitSelectSyntax(SelectSyntax pNode)
        {
            var rw = _rewrite;
            _rewrite = false;

            //Save itVar in case we hit a nested for or select statement
            var it = _itVar;
            _itVar = pNode.Condition;
            SyntaxNode retval = base.VisitSelectSyntax(pNode);

            if (_rewrite)
            {
                if(pNode.Annotation.Value == KeyAnnotations.Complete)
                {
                    CompilerErrors.IgnoredComplete(pNode.Span);
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
                        SyntaxNode baseExpression = Visit(currentCase.Conditions[0]);
                        if (!IsComparison(baseExpression))
                        {
                            //If it isn't make it one
                            baseExpression = SyntaxFactory.BinaryExpression(_itVar, BinaryExpressionOperator.Equals, baseExpression);
                            ((BinaryExpressionSyntax)baseExpression).SetType(SmallTypeCache.Boolean);
                        }

                        for (int j = 0; j < currentCase.Conditions.Count - 1; j++)
                        {
                            var newExpression = currentCase.Conditions[j + 1];
                            if (!IsComparison(newExpression))
                            {
                                //If it isn't make it one
                                newExpression = SyntaxFactory.BinaryExpression(_itVar, BinaryExpressionOperator.Equals, newExpression);
                                ((BinaryExpressionSyntax)newExpression).SetType(SmallTypeCache.Boolean);
                            }

                            baseExpression = SyntaxFactory.BinaryExpression(baseExpression, BinaryExpressionOperator.Or, newExpression);
                            ((BinaryExpressionSyntax)baseExpression).SetType(SmallTypeCache.Boolean);
                        }

                        //Visit body so we can rewrite any "it"
                        var b = (BlockSyntax)Visit(currentCase.Body);
                        _currentIf = SyntaxFactory.If(baseExpression, b, _currentElse);

                        if (i > 0)
                        {
                            //As long as this isn't the last statement, make an else if
                            _currentElse = SyntaxFactory.Else(_currentIf, null);
                        }
                    }
                }

                retval = _currentIf;
            }

            _itVar = it;
            _rewrite = rw;
            return retval;
        }

        protected override SyntaxNode VisitItSyntax(ItSyntax pNode)
        {
            _rewrite = true;
            return _itVar ?? pNode;
        }

        private bool IsComparison(SyntaxNode pNode)
        {
            if (pNode.SyntaxType != SyntaxType.BinaryExpression) return false;
            var op = ((BinaryExpressionSyntax)pNode).Operator;
            return op == BinaryExpressionOperator.Equals ||
                   op == BinaryExpressionOperator.GreaterThan ||
                   op == BinaryExpressionOperator.GreaterThanOrEqual ||
                   op == BinaryExpressionOperator.LessThan ||
                   op == BinaryExpressionOperator.LessThanOrEqual ||
                   op == BinaryExpressionOperator.NotEquals ||
                   op == BinaryExpressionOperator.And ||
                   op == BinaryExpressionOperator.Or;
        }
    }
}
