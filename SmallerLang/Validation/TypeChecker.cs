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
        SmallType _casetype;
        SmallType _currentType;

        public TypeChecker(IErrorReporter pError)
        {
            _error = pError;
        }

        protected override void VisitArrayAccessSyntax(ArrayAccessSyntax pNode)
        {
            if(!pNode.Index.Type.IsAssignableFrom(SmallTypeCache.Int))
            {
                _error.WriteError($"Type of {pNode.Index.Type.ToString()} cannot be converted to {SmallTypeCache.Int.ToString()}", pNode.Index.Span);
            }
            if(!pNode.BaseType.IsArray)
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
                var t = isTuple ? pNode.Value.Type.GetFieldType(i) : pNode.Value.Type;

                if (!pNode.Variables[i].Type.IsAssignableFrom(t))
                {
                    _error.WriteError($"Type of {pNode.Variables[i].Type.ToString()} cannot be converted to {t.ToString()}", pNode.Span);
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
                    if (!pNode.Left.Type.IsAssignableFrom(SmallTypeCache.Boolean))
                    {
                        _error.WriteError($"Type of {pNode.Left.Type.ToString()} cannot be converted to {SmallTypeCache.Boolean.ToString()}", pNode.Span);
                    }
                    if(!pNode.Right.Type.IsAssignableFrom(SmallTypeCache.Boolean))
                    {
                        _error.WriteError($"Type of {pNode.Right.Type.ToString()} cannot be converted to {SmallTypeCache.Boolean.ToString()}", pNode.Span);
                    }
                    break;

                default:
                    if(BinaryExpressionSyntax.GetResultType(pNode.Left.Type, pNode.Operator, pNode.Right.Type) == SmallTypeCache.Undefined)
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
                    if(!pNode.Value.Type.IsAssignableFrom(SmallTypeCache.Boolean))
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

        protected override void VisitMemberAccessSyntax(MemberAccessSyntax pNode)
        {
            var t = _currentType;
            _currentType = pNode.Identifier.Type;

            base.VisitMemberAccessSyntax(pNode);
            _currentType = t;
        }

        protected override void VisitMethodCallSyntax(MethodCallSyntax pNode)
        {
            SmallType[] types = Utils.SyntaxHelper.SelectNodeTypes(pNode.Arguments);

            var methodFound = FindMethod(out MethodDefinition m, pNode.Value, _currentType, types);
            System.Diagnostics.Debug.Assert(methodFound, "Something went very, very wrong...");

            for(int i = 0; i < m.ArgumentTypes.Count; i++)
            {
                if(!pNode.Arguments[i].Type.IsAssignableFrom(m.ArgumentTypes[i]))
                {
                    _error.WriteError($"Type of {pNode.Arguments[i].Type.ToString()} cannot be converted to {m.ArgumentTypes[i].ToString()}", pNode.Arguments[i].Span);
                }
            }

            //Method calls are finally validated, set the mangled method name which we will actually call
            pNode.SetDefinition(m);
            base.VisitMethodCallSyntax(pNode);
        }

        private bool FindMethod(out MethodDefinition pDef, string pName, SmallType pType, params SmallType[] pArguments)
        {
            MethodCache.FindMethod(out pDef, pType, pName, pArguments);
            if (pDef.Name == null)
            {
                foreach (var trait in pType.Implements)
                {
                    MethodCache.FindMethod(out pDef, trait, pName, pArguments);
                    if (pDef.Name != null) return true;
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
            else if(pNode.Struct.Type.HasDefinedConstructor())
            {
                var ctor = pNode.Struct.Type.GetConstructor().Name;
                MethodCache.FindMethod(out MethodDefinition m, pNode.Struct.Type, ctor, types);
                for (int i = 0; i < m.ArgumentTypes.Count; i++)
                {
                    if (!pNode.Arguments[i].Type.IsAssignableFrom(m.ArgumentTypes[i]))
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
            _methodReturns = Utils.SyntaxHelper.SelectNodeTypes(pNode.ReturnValues);
            base.VisitMethodSyntax(pNode);
        }

        protected override void VisitReturnSyntax(ReturnSyntax pNode)
        {
            for(int i = 0; i < pNode.Values.Count; i++)
            {
                if (!pNode.Values[i].Type.IsAssignableFrom(_methodReturns[i]))
                {
                    _error.WriteError($"Type of {pNode.Values[i].Type.ToString()} cannot be converted to {_methodReturns[i].ToString()}", pNode.Values[i].Span);
                }
            }
            base.VisitReturnSyntax(pNode);
        }

        protected override void VisitForSyntax(ForSyntax pNode)
        {
            if (!pNode.Condition.Type.IsAssignableFrom(SmallTypeCache.Boolean))
            {
                _error.WriteError($"Type of {pNode.Condition.Type.ToString()} cannot be converted to {SmallTypeCache.Boolean.ToString()}", pNode.Condition.Span);
            }
            base.VisitForSyntax(pNode);
        }

        protected override void VisitIfSyntax(IfSyntax pNode)
        {
            if (!pNode.Condition.Type.IsAssignableFrom(SmallTypeCache.Boolean))
            {
                _error.WriteError($"Type of {pNode.Condition.Type.ToString()} cannot be converted to {SmallTypeCache.Boolean.ToString()}", pNode.Condition.Span);
            }
            base.VisitIfSyntax(pNode);
        }

        protected override void VisitWhileSyntax(WhileSyntax pNode)
        {
            if(!pNode.Condition.Type.IsAssignableFrom(SmallTypeCache.Boolean))
            {
                _error.WriteError($"Type of {pNode.Condition.Type.ToString()} cannot be converted to {SmallTypeCache.Boolean.ToString()}", pNode.Condition.Span);
            }
            base.VisitWhileSyntax(pNode);
        }

        protected override void VisitSelectSyntax(SelectSyntax pNode)
        {
            _casetype = pNode.Condition.Type;
            base.VisitSelectSyntax(pNode);
        }

        protected override void VisitCaseSyntax(CaseSyntax pNode)
        {
            foreach(var c in pNode.Conditions)
            {
                if(!_casetype.IsAssignableFrom(c.Type))
                {
                    _error.WriteError($"Type of {c.Type.ToString()} cannot be converted to {_casetype.ToString()}", pNode.Span);
                }
            }
            base.VisitCaseSyntax(pNode);
        }
    }
}
