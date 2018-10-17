using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;
using SmallerLang.Emitting;

namespace SmallerLang.Validation
{
    class TypeChecker : SyntaxNodeVisitor
    {
        readonly IErrorReporter _error;
        SmallType[] _methodReturns;

        public TypeChecker(IErrorReporter pError)
        {
            _error = pError;
        }

        protected override void VisitModuleSyntax(ModuleSyntax pNode)
        {
            base.VisitModuleSyntax(pNode);
        }

        protected override void VisitArrayAccessSyntax(ArrayAccessSyntax pNode)
        {
            if(!CanCast(pNode.Index.Type, SmallTypeCache.Int))
            {
                _error.WriteError($"Type of {pNode.Index.Type.ToString()} cannot be converted to {SmallTypeCache.Int.ToString()}", pNode.Index.Span);
            }
            if(!pNode.Identifier.Type.IsArray)
            {
                _error.WriteError($"Array access can only be on array types", pNode.Span);
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
                    _error.WriteError($"Type of {pNode.Variables[i].Type.ToString()} cannot be converted to {valueType.ToString()}", pNode.Span);
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
                        _error.WriteError($"Type of {pNode.Left.Type.ToString()} cannot be converted to {SmallTypeCache.Boolean.ToString()}", pNode.Span);
                    }
                    if(!CanCast(pNode.Right.Type, SmallTypeCache.Boolean))
                    {
                        _error.WriteError($"Type of {pNode.Right.Type.ToString()} cannot be converted to {SmallTypeCache.Boolean.ToString()}", pNode.Span);
                    }
                    break;

                default:
                    //Types can be undefined if the type was not found.
                    //These errors will be reported when the type is first found
                    if (pNode.Left.Type != SmallTypeCache.Undefined && 
                        pNode.Right.Type != SmallTypeCache.Undefined &&
                        BinaryExpressionSyntax.GetResultType(pNode.Left.Type, pNode.Operator, pNode.Right.Type) == SmallTypeCache.Undefined)
                    {
                        _error.WriteError($"Type of {pNode.Left.Type.ToString()} cannot be converted to {pNode.Right.Type.ToString()}", pNode.Span);
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
                        _error.WriteError($"Type of {pNode.Value.Type.ToString()} cannot be converted to {SmallTypeCache.Boolean.ToString()}", pNode.Value.Span);
                    }
                    break;

                case UnaryExpressionOperator.Length:
                    if(!pNode.Value.Type.IsArray)
                    {
                        _error.WriteError("lengthof can only be used on array types", pNode.Span);
                    }
                    break;

                case UnaryExpressionOperator.PreDecrement:
                case UnaryExpressionOperator.PreIncrement:
                case UnaryExpressionOperator.PostDecrement:
                case UnaryExpressionOperator.PostIncrement:
                case UnaryExpressionOperator.Negative:
                    if(!Utils.TypeHelper.IsNumber(pNode.Value.Type))
                    {
                        _error.WriteError($"{pNode.Value.Type.ToString()} is not a numeric type", pNode.Span);
                    }
                    break;
            }
            base.VisitUnaryExpressionSyntax(pNode);
        }

        protected override void VisitMethodCallSyntax(MethodCallSyntax pNode)
        {
            SmallType[] types = Utils.SyntaxHelper.SelectNodeTypes(pNode.Arguments);

            //Current type can be undefined if we have a method call on a type that isn't defined
            if(Type != SmallTypeCache.Undefined)
            {
                var methodFound = FindMethod(out MethodDefinition m, pNode.Value, Type, types);
                System.Diagnostics.Debug.Assert(methodFound, "Something went very, very wrong...");

                for (int i = 0; i < m.ArgumentTypes.Count; i++)
                {
                    if (!CanCast(pNode.Arguments[i].Type, m.ArgumentTypes[i]))
                    {
                        _error.WriteError($"Type of {pNode.Arguments[i].Type.ToString()} cannot be converted to {m.ArgumentTypes[i].ToString()}", pNode.Arguments[i].Span);
                    }
                }

                //Method calls are finally validated, set the mangled method name which we will actually call
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
            SmallType[] types = Utils.SyntaxHelper.SelectNodeTypes(pNode.Arguments);

            if(pNode.Struct.Type == SmallTypeCache.Undefined)
            {
                _error.WriteError($"Use of undeclared type {pNode.Struct.Value}", pNode.Span);
            }
            else if(pNode.Struct.Type.IsTrait)
            {
                _error.WriteError("Traits cannot be directly initialized", pNode.Span);
            }
            else if(pNode.Struct.Type.HasDefinedConstructor())
            {
                var ctor = pNode.Struct.Type.GetConstructor().Name;
                MethodCache.FindMethod(out MethodDefinition m, Namespace, pNode.Struct.Type, ctor, types);
                for (int i = 0; i < m.ArgumentTypes.Count; i++)
                {
                    if (!CanCast(pNode.Arguments[i].Type, m.ArgumentTypes[i]))
                    {
                        _error.WriteError($"Type of {pNode.Arguments[i].Type.ToString()} cannot be converted to {m.ArgumentTypes[i].ToString()}", pNode.Arguments[i].Span);
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
                    _error.WriteError($"Use of undefined type {pNode.ReturnValues[i].Value}", pNode.ReturnValues[i].Span);
                }
            }
            _methodReturns = Utils.SyntaxHelper.SelectNodeTypes(pNode.ReturnValues);
            base.VisitMethodSyntax(pNode);
        }

        protected override void VisitReturnSyntax(ReturnSyntax pNode)
        {
            for(int i = 0; i < pNode.Values.Count; i++)
            {
                if (!CanCast(pNode.Values[i].Type, _methodReturns[i]))
                {
                    _error.WriteError($"Type of {pNode.Values[i].Type.ToString()} cannot be converted to {_methodReturns[i].ToString()}", pNode.Values[i].Span);
                }
            }
            base.VisitReturnSyntax(pNode);
        }

        protected override void VisitForSyntax(ForSyntax pNode)
        {
            if (!CanCast(pNode.Condition.Type, SmallTypeCache.Boolean))
            {
                _error.WriteError($"Type of {pNode.Condition.Type.ToString()} cannot be converted to {SmallTypeCache.Boolean.ToString()}", pNode.Condition.Span);
            }
            base.VisitForSyntax(pNode);
        }

        protected override void VisitIfSyntax(IfSyntax pNode)
        {
            if (!CanCast(pNode.Condition.Type, SmallTypeCache.Boolean))
            {
                _error.WriteError($"Type of {pNode.Condition.Type.ToString()} cannot be converted to {SmallTypeCache.Boolean.ToString()}", pNode.Condition.Span);
            }
            base.VisitIfSyntax(pNode);
        }

        protected override void VisitWhileSyntax(WhileSyntax pNode)
        {
            if(!CanCast(pNode.Condition.Type, SmallTypeCache.Boolean))
            {
                _error.WriteError($"Type of {pNode.Condition.Type.ToString()} cannot be converted to {SmallTypeCache.Boolean.ToString()}", pNode.Condition.Span);
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
                    _error.WriteError($"Type of {c.Type.ToString()} cannot be converted to {caseType.ToString()}", pNode.Span);
                }
            }
            base.VisitCaseSyntax(pNode);
        }

        protected override void VisitTypedIdentifierSyntax(TypedIdentifierSyntax pNode)
        {
            if (pNode.Type == SmallTypeCache.Undefined)
            {
                _error.WriteError($"Use of undefined type {pNode.TypeNode.Value}", pNode.Span);
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
