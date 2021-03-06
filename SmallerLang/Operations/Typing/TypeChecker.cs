﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;
using SmallerLang.Emitting;
using SmallerLang.Utils;

namespace SmallerLang.Operations.Typing
{
    class TypeChecker : SyntaxNodeVisitor
    {
        SmallType[] _methodReturns;
        readonly Compiler.CompilationCache _unit;

        public TypeChecker(Compiler.CompilationCache pUnit)
        {
            _unit = pUnit;
        }

        protected override void VisitArrayAccessSyntax(ArrayAccessSyntax pNode)
        {
            if(!CanCast(pNode.Index.Type, SmallTypeCache.Int))
            {
                CompilerErrors.TypeCastError(pNode.Index.Type, SmallTypeCache.Int, pNode.Index.Span);
            }

            if(!pNode.Identifier.Type.IsArray)
            {
                CompilerErrors.CannotIndex(pNode.Identifier.Type, pNode.Span);
            }

            base.VisitArrayAccessSyntax(pNode);
        }

        protected override void VisitArrayLiteralSyntax(ArrayLiteralSyntax pNode)
        {
            if(pNode.Type == SmallTypeCache.Undefined)
            {
                CompilerErrors.UndeclaredType(pNode.TypeNode.Value, pNode.Span);
            }
            base.VisitArrayLiteralSyntax(pNode);
        }

        protected override void VisitAssignmentSyntax(AssignmentSyntax pNode)
        {
            var isTuple = pNode.Value.Type.IsTuple;
            for (int i = 0; i < pNode.Variables.Count; i++)
            {
                var valueType = isTuple ? pNode.Value.Type.GetFieldType(i) : pNode.Value.Type;

                if (!CanCast(pNode.Variables[i].Type, valueType))
                {
                    CompilerErrors.TypeCastError(pNode.Variables[i].Type, valueType, pNode.Span);
                }
            }
            base.VisitAssignmentSyntax(pNode);
        }

        protected override void VisitTernaryExpression(TernaryExpressionSyntax pNode)
        {
            if(!CanCast(pNode.Condition.Type, SmallTypeCache.Boolean))
            {
                CompilerErrors.TypeCastError(pNode.Condition.Type, SmallTypeCache.Boolean, pNode.Condition.Span);
            }
            if (pNode.Left.Type != pNode.Right.Type)
            {
                CompilerErrors.TypeCastError(pNode.Left.Type, pNode.Right.Type, pNode.Span);
            }
            base.VisitTernaryExpression(pNode);
        }

        protected override void VisitBinaryExpressionSyntax(BinaryExpressionSyntax pNode)
        {
            switch(pNode.Operator)
            {
                case BinaryExpressionOperator.And:
                case BinaryExpressionOperator.Or:
                    if (!CanCast(pNode.Left.Type, SmallTypeCache.Boolean))
                    {
                        CompilerErrors.TypeCastError(pNode.Left.Type, SmallTypeCache.Boolean, pNode.Span);
                    }
                    if(!CanCast(pNode.Right.Type, SmallTypeCache.Boolean))
                    {
                        CompilerErrors.TypeCastError(pNode.Right.Type, SmallTypeCache.Boolean, pNode.Span);
                    }
                    break;

                case BinaryExpressionOperator.BitwiseAnd:
                case BinaryExpressionOperator.BitwiseOr:
                    if (!CanCast(pNode.Left.Type, SmallTypeCache.Int))
                    {
                        CompilerErrors.TypeCastError(pNode.Left.Type, SmallTypeCache.Int, pNode.Span);
                    }
                    if (!CanCast(pNode.Right.Type, SmallTypeCache.Int))
                    {
                        CompilerErrors.TypeCastError(pNode.Right.Type, SmallTypeCache.Int, pNode.Span);
                    }
                    break;

                case BinaryExpressionOperator.LeftBitShift:
                case BinaryExpressionOperator.RightBitShift:
                    if(pNode.Left.Type != SmallTypeCache.Undefined &&
                       pNode.Right.Type != SmallTypeCache.Undefined && 
                       !CanCast(pNode.Left.Type, pNode.Right.Type))
                    {
                        CompilerErrors.TypeCastError(pNode.Left.Type, pNode.Right.Type, pNode.Span);
                    }
                    break;

                default:
                    //Types can be undefined if the type was not found.
                    //These errors will be reported when the type is first found
                    if (pNode.Left.Type != SmallTypeCache.Undefined && 
                        pNode.Right.Type != SmallTypeCache.Undefined &&
                        BinaryExpressionSyntax.GetResultType(pNode.Left.Type, pNode.Operator, pNode.Right.Type) == SmallTypeCache.Undefined)
                    {
                        CompilerErrors.TypeCastError(pNode.Left.Type, pNode.Right.Type, pNode.Span);
                    }
                    break;
            }

            base.VisitBinaryExpressionSyntax(pNode);
        }

        protected override void VisitUnaryExpressionSyntax(UnaryExpressionSyntax pNode)
        {
            switch (pNode.Operator)
            {
                case UnaryExpressionOperator.Not:
                    if(!CanCast(pNode.Value.Type, SmallTypeCache.Boolean))
                    {
                        CompilerErrors.TypeCastError(pNode.Value.Type, SmallTypeCache.Boolean, pNode.Value.Span);
                    }
                    break;

                case UnaryExpressionOperator.Length:
                    if(!pNode.Value.Type.IsArray)
                    {
                        CompilerErrors.TypeCastError(pNode.Value.Type.ToString(), "array", pNode.Span);
                    }
                    break;

                case UnaryExpressionOperator.PreDecrement:
                case UnaryExpressionOperator.PreIncrement:
                case UnaryExpressionOperator.PostDecrement:
                case UnaryExpressionOperator.PostIncrement:
                case UnaryExpressionOperator.Negative:
                    if(!TypeHelper.IsInt(pNode.Value.Type))
                    {
                        CompilerErrors.TypeCastError(pNode.Value.Type, SmallTypeCache.Int, pNode.Span);
                    }
                    break;
            }
            base.VisitUnaryExpressionSyntax(pNode);
        }

        protected override void VisitMethodCallSyntax(MethodCallSyntax pNode)
        {
            SmallType[] types = SyntaxHelper.SelectNodeTypes(pNode.Arguments);

            //Current type can be undefined if we have a method call on a type that isn't defined
            if (CurrentType != SmallTypeCache.Undefined)
            {
                var methodFound = SyntaxHelper.FindMethodOnType(out MethodDefinition m, _unit, Namespace, pNode.Value, CurrentType, types);
                System.Diagnostics.Debug.Assert(methodFound == Compiler.FindResult.Found, "This shouldn't have happened...");

                //Method calls are finally validated, set the mangled method name which we will actually call
                m = m.MakeConcreteDefinition(CurrentType);
                pNode.SetDefinition(m);
            }

            base.VisitMethodCallSyntax(pNode);
        }

        protected override void VisitStructInitializerSyntax(StructInitializerSyntax pNode)
        {
            //Check if the type exists
            if(pNode.Struct.Type == SmallTypeCache.Undefined)
            {
                CompilerErrors.UndeclaredType(pNode.Struct.ToString(), pNode.Span);
            }
            //Check if the type is a trait
            else if(pNode.Struct.Type.IsTrait)
            {
                CompilerErrors.AttemptDeclareTrait(pNode.Struct.Type, pNode.Span);
            }
            //Ensure the constructor arguments 
            else if(pNode.Struct.Type.HasDefinedConstructor())
            {
                var m = pNode.Struct.Type.GetConstructor();
                for (int i = 0; i < m.ArgumentTypes.Count; i++)
                {
                    if (!CanCast(pNode.Arguments[i].Type, m.ArgumentTypes[i]))
                    {
                        CompilerErrors.TypeCastError(pNode.Arguments[i].Type, m.ArgumentTypes[i], pNode.Arguments[i].Span);
                    }
                }
            }

            base.VisitStructInitializerSyntax(pNode);
        }

        protected override void VisitCastDefinitionSyntax(CastDefinitionSyntax pNode)
        {
            _methodReturns = new SmallType[] { pNode.Type };
            base.VisitCastDefinitionSyntax(pNode);
        }

        protected override void VisitMethodSyntax(MethodSyntax pNode)
        {
            for(int i = 0; i < pNode.ReturnValues.Count; i++)
            {
                if(pNode.ReturnValues[i].Type == SmallTypeCache.Undefined)
                {
                    CompilerErrors.UndeclaredType(pNode.ReturnValues[i].Value, pNode.ReturnValues[i].Span);
                }
            }
            _methodReturns = SyntaxHelper.SelectNodeTypes(pNode.ReturnValues);
            base.VisitMethodSyntax(pNode);
        }

        protected override void VisitReturnSyntax(ReturnSyntax pNode)
        {
            for(int i = 0; i < Math.Min(_methodReturns.Length, pNode.Values.Count); i++)
            {
                if (!CanCast(pNode.Values[i].Type, _methodReturns[i]))
                {
                    CompilerErrors.TypeCastError(pNode.Values[i].Type, _methodReturns[i], pNode.Values[i].Span);
                }
            }
            base.VisitReturnSyntax(pNode);
        }

        protected override void VisitForSyntax(ForSyntax pNode)
        {
            if(pNode.Iterator != null)
            {
                if(SmallTypeCache.TryGetEnumerable(_unit, out SmallType enumerable))
                {
                    if (!pNode.Iterator.Type.IsArray && !CanCast(pNode.Iterator.Type, enumerable))
                    {
                        CompilerErrors.IteratorError(pNode.Iterator.Type, pNode.Iterator.Span);
                    }
                }
            }
            else if(!CanCast(pNode.Condition.Type, SmallTypeCache.Boolean))
            {
                CompilerErrors.TypeCastError(pNode.Condition.Type, SmallTypeCache.Boolean, pNode.Condition.Span);
            }

            base.VisitForSyntax(pNode);
        }

        protected override void VisitIfSyntax(IfSyntax pNode)
        {
            if (!CanCast(pNode.Condition.Type, SmallTypeCache.Boolean))
            {
                CompilerErrors.TypeCastError(pNode.Condition.Type, SmallTypeCache.Boolean, pNode.Condition.Span);
            }
            base.VisitIfSyntax(pNode);
        }

        protected override void VisitWhileSyntax(WhileSyntax pNode)
        {
            if(!CanCast(pNode.Condition.Type, SmallTypeCache.Boolean))
            {
                CompilerErrors.TypeCastError(pNode.Condition.Type, SmallTypeCache.Boolean, pNode.Condition.Span);
            }
            base.VisitWhileSyntax(pNode);
        }

        protected override void VisitSelectSyntax(SelectSyntax pNode)
        {
            using (var c = Store.AddValue("CaseType", pNode.Condition.Type))
            {
                base.VisitSelectSyntax(pNode);
            }
        }

        protected override void VisitCaseSyntax(CaseSyntax pNode)
        {
            var caseType = Store.GetValue<SmallType>("CaseType");
            foreach(var c in pNode.Conditions)
            {
                if(!CanCast(caseType, c.Type))
                {
                    CompilerErrors.TypeCastError(c.Type, caseType, pNode.Span);
                }
            }
            base.VisitCaseSyntax(pNode);
        }

        protected override void VisitTypedIdentifierSyntax(TypedIdentifierSyntax pNode)
        {
            if (pNode.Type == SmallTypeCache.Undefined)
            {
                CompilerErrors.UndeclaredType(pNode.TypeNode.Value, pNode.Span);
            }
            base.VisitTypedIdentifierSyntax(pNode);
        }

        private bool CanCast(SmallType pFrom, SmallType pTo)
        {
            //Undefined types are caused by non-existent types
            //These errors will be caught when the type is first encountered
            return pTo.IsAssignableFrom(pFrom);
        }
    }
}
