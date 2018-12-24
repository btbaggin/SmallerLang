using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;
using SmallerLang.Emitting;
using SmallerLang.Utils;

namespace SmallerLang.Validation
{
    class TypeChecker : SyntaxNodeVisitor
    {
        SmallType[] _methodReturns;

        protected override void VisitArrayAccessSyntax(ArrayAccessSyntax pNode)
        {
            if(!CanCast(pNode.Index.Type, SmallTypeCache.Int))
            {
                CompilerErrors.TypeCastError(pNode.Index.Type, SmallTypeCache.Int, pNode.Index.Span);
            }
            if(!pNode.Identifier.Type.IsArray)
            {
                CompilerErrors.TypeCastError(pNode.Identifier.Type.ToString(), "array", pNode.Span);
            }
            base.VisitArrayAccessSyntax(pNode);
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
                    if(!TypeHelper.IsNumber(pNode.Value.Type))
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
            if (Type != SmallTypeCache.Undefined)
            {
                var methodFound = FindMethod(out MethodDefinition m, pNode.Value, Type, types);
                System.Diagnostics.Debug.Assert(methodFound, "Something went very, very wrong...");

                if (!methodFound)
                {
                    //Parameters are type-checked in FindMethod but this will give us good error messages
                    for (int i = 0; i < m.ArgumentTypes.Count; i++)
                    {
                        if (!CanCast(pNode.Arguments[i].Type, m.ArgumentTypes[i]))
                        {
                            CompilerErrors.TypeCastError(pNode.Arguments[i].Type, m.ArgumentTypes[i], pNode.Arguments[i].Span);
                        }
                    }
                }

                //Method calls are finally validated, set the mangled method name which we will actually call
                m = m.MakeConcreteDefinition(Type);
                pNode.SetDefinition(m);
            }

            base.VisitMethodCallSyntax(pNode);
        }

        private bool FindMethod(out MethodDefinition pDef, string pName,  SmallType pType, params SmallType[] pArguments)
        {
            MethodCache.FindMethod(out pDef, out bool pExact, Namespace, pType, pName, pArguments);
            if (!pExact)
            {
                if (pType != null)
                {
                    foreach (var trait in pType.Implements)
                    {
                        MethodCache.FindMethod(out pDef, Namespace, trait, pName, pArguments);
                        if (pDef.Name != null) return true;
                    }
                }
                
                return false;
            }

            return true;
        }

        protected override void VisitStructInitializerSyntax(StructInitializerSyntax pNode)
        {
            SmallType[] types = SyntaxHelper.SelectNodeTypes(pNode.Arguments);

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
                var ctor = pNode.Struct.Type.GetConstructor().Name;
                MethodCache.FindMethod(out MethodDefinition m, Namespace, pNode.Struct.Type, ctor, types);
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
            if (!CanCast(pNode.Condition.Type, SmallTypeCache.Boolean))
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
            return pFrom == SmallTypeCache.Undefined || pTo == SmallTypeCache.Undefined || pFrom.IsAssignableFrom(pTo);
        }
    }
}
