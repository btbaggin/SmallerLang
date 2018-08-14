using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;
using SmallerLang.Emitting;

namespace SmallerLang.Validation
{
    partial class PostTypeValidation : SyntaxNodeVisitor
    {
        readonly IErrorReporter _error;
        private bool _returnFound;
        private int _returnValueCount;

        public PostTypeValidation(IErrorReporter pError)
        {
            _error = pError;
        }

        protected override void VisitMethodSyntax(MethodSyntax pNode)
        {
            ValidateRun(pNode);
            ValidateReturn(pNode);
        }

        private void ValidateReturn(MethodSyntax pNode)
        {
            _returnFound = false;
            _returnValueCount = pNode.ReturnValues.Count;
            base.VisitMethodSyntax(pNode);

            if (pNode.ReturnValues.Count != 0 && !_returnFound)
            {
                _error.WriteError("Not all code paths return a value", pNode.Span);
            }
            else if (pNode.ReturnValues.Count == 0 && _returnFound)
            {
                _error.WriteError("Method has no return value, so no return statement must be present", pNode.Span);
            }
        }

        protected override void VisitCastSyntax(CastSyntax pNode)
        {
            base.VisitCastSyntax(pNode);

            if(!pNode.FromType.IsAssignableFrom(pNode.Type) &&
               (!IsStandard(pNode.FromType) || !IsStandard(pNode.Type)))
            {
                if (MethodCache.CastExists(pNode.FromType, pNode.Type, out MethodDefinition d))
                {
                    pNode.SetMethod(d);
                }
                else
                {
                    _error.WriteError($"No cast defined for types {pNode.FromType.ToString()} and {pNode.Type.ToString()}");
                }
            }
        }

        protected override void VisitCastDefinitionSyntax(CastDefinitionSyntax pNode)
        {
            _returnFound = false;
            _returnValueCount = pNode.ReturnValues.Count;
            base.VisitCastDefinitionSyntax(pNode);

            if (pNode.ReturnValues.Count != 0 && !_returnFound)
            {
                _error.WriteError("Not all code paths return a value", pNode.Span);
            }
            else if (pNode.ReturnValues.Count == 0 && _returnFound)
            {
                _error.WriteError("Method has no return value, so no return statement must be present", pNode.Span);
            }

            if (MethodCache.CastCount(pNode.Parameters[0].Type, pNode.Type) != 1)
            {
                _error.WriteError($"Multiple definitions for cast definition from type {pNode.Parameters[0].Type.ToString()} to {pNode.Type.ToString()} found");
            }
        }

        protected override void VisitIfSyntax(IfSyntax pNode)
        {
            Visit((dynamic)pNode.Condition);
            Visit(pNode.Body);

            if(pNode.Else != null)
            {
                var found = _returnFound;
                _returnFound = false;

                Visit(pNode.Else);

                //Returns must be found in ALL else blocks
                _returnFound &= found;
            }
        }

        protected override void VisitReturnSyntax(ReturnSyntax pNode)
        {
            _returnFound = true;
            if(pNode.Values.Count != _returnValueCount)
            {
                _error.WriteError("Method must return " + _returnValueCount + " values", pNode.Span);
            }

            if (pNode.Deferred)
            {
                _error.WriteError("Unable to defer a return statement", pNode.Span);
            }
            base.VisitReturnSyntax(pNode);
        }

        private static bool IsStandard(SmallType pType)
        {
            return pType == SmallTypeCache.Short ||
                   pType == SmallTypeCache.Int ||
                   pType == SmallTypeCache.Long ||
                   pType == SmallTypeCache.Float ||
                   pType == SmallTypeCache.Double ||
                   pType == SmallTypeCache.String ||
                   pType == SmallTypeCache.Boolean;
        }
    }
}
