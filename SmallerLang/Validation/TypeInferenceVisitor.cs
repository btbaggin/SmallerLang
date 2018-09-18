using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;
using SmallerLang.Emitting;

namespace SmallerLang.Validation
{
    class TypeInferenceVisitor : SyntaxNodeVisitor
    {
        SmallType _currentType;
        SmallType _currentStruct;
        VariableCache<SmallType> _locals;
        readonly IErrorReporter _error;
        SmallType[] _methodReturns;

        public TypeInferenceVisitor(IErrorReporter pError)
        {
            _locals = new VariableCache<SmallType>();
            _error = pError;
        }

        protected override void VisitModuleSyntax(ModuleSyntax pNode)
        {
            //Infer methods
            foreach (var m in pNode.Methods)
            {
                Visit(m);
            }

            foreach (var s in pNode.Structs)
            {
                _currentStruct = SmallTypeCache.FromString(s.Name);
                foreach (var m in s.Methods)
                {
                    Visit(m);
                }
            }
        }
   
        protected override void VisitDeclarationSyntax(DeclarationSyntax pNode)
        {
            for (int i = 0; i < pNode.Variables.Count; i++)
            {
                if(!Utils.SyntaxHelper.IsDiscard(pNode.Variables[i]))
                {
                    if (_locals.IsVariableDefinedInScope(pNode.Variables[i].Value))
                    {
                        _error.WriteError("Local variable named '" + pNode.Variables[i].Value + "' is already defined in this scope", pNode.Span);
                    }
                    else
                    {
                        if (SmallTypeCache.IsTypeDefined(pNode.Variables[i].Value))
                        {
                            _error.WriteError("Value is already defined as a type", pNode.Variables[i].Span);
                        }
                        else
                        {
                            Visit((dynamic)pNode.Value);

                            //For tuple types we set the individual variables to the tuple field type... not the tuple itself
                            var isTuple = pNode.Value.Type.IsTuple;
                            var t = isTuple ? pNode.Value.Type.GetFieldType(i) : pNode.Value.Type;

                            pNode.Variables[i].SetType(t);
                            _locals.DefineVariableInScope(pNode.Variables[i].Value, pNode.Variables[i].Type);

                            if (isTuple && pNode.Value.Type.GetFieldCount() != pNode.Variables.Count)
                            {
                                _error.WriteError($"Value returns {pNode.Value.Type.GetFieldCount()} values but {pNode.Variables.Count} are specified", pNode.Span);
                            }
                        }
                    }
                }
            }
        }

        protected override void VisitAssignmentSyntax(AssignmentSyntax pNode)
        {
            base.VisitAssignmentSyntax(pNode);

            var isTuple = pNode.Value.Type.IsTuple;
            for (int i = 0; i < pNode.Variables.Count; i++)
            {
                var t = isTuple ? pNode.Value.Type.GetFieldType(i) : pNode.Value.Type;

                //We have to set the type of discards so the tuple is created properly
                if (Utils.SyntaxHelper.IsDiscard(pNode.Variables[i])) ((DiscardSyntax)pNode.Variables[i]).SetType(t);
            }
        }

        protected override void VisitStructInitializerSyntax(StructInitializerSyntax pNode)
        {
            base.VisitStructInitializerSyntax(pNode);

            var m = pNode.Type.GetConstructor();
            if(m.ArgumentTypes != null && m.ArgumentTypes.Count != pNode.Arguments.Count)
            {
                _error.WriteError($"Constructor to {pNode.Type.Name} is expecting {m.ArgumentTypes.Count.ToString()} argument(s) but has {pNode.Arguments.Count.ToString()}", pNode.Span);
            }
        }

        protected override void VisitBinaryExpressionSyntax(BinaryExpressionSyntax pNode)
        {
            base.VisitBinaryExpressionSyntax(pNode);

            ForceCastLiteral(pNode.Left.Type, pNode.Right);
            ForceCastLiteral(pNode.Right.Type, pNode.Left);

            switch (pNode.Operator)
            {
                case BinaryExpressionOperator.Addition:
                case BinaryExpressionOperator.Subtraction:
                case BinaryExpressionOperator.Multiplication:
                case BinaryExpressionOperator.Division:
                case BinaryExpressionOperator.Mod:
                    pNode.SetType(BinaryExpressionSyntax.GetResultType(pNode.Left.Type, pNode.Operator, pNode.Right.Type));
                    break;

                case BinaryExpressionOperator.Equals:
                case BinaryExpressionOperator.NotEquals:
                case BinaryExpressionOperator.LessThan:
                case BinaryExpressionOperator.LessThanOrEqual:
                case BinaryExpressionOperator.GreaterThan:
                case BinaryExpressionOperator.GreaterThanOrEqual:
                case BinaryExpressionOperator.And:
                case BinaryExpressionOperator.Or:
                    pNode.SetType(SmallTypeCache.Boolean);
                    break;

                default:
                    pNode.SetType(SmallTypeCache.Undefined);
                    break;
            }

            TrySetImplicitCastType(pNode.Left, pNode.Right.Type);
            TrySetImplicitCastType(pNode.Right, pNode.Left.Type);
        }

        protected override void VisitUnaryExpressionSyntax(UnaryExpressionSyntax pNode)
        {
            base.VisitUnaryExpressionSyntax(pNode);

            switch(pNode.Operator)
            {
                case UnaryExpressionOperator.Not:
                    TrySetImplicitCastType(pNode.Value, SmallTypeCache.Boolean);
                    pNode.SetType(SmallTypeCache.Boolean);
                    break;

                case UnaryExpressionOperator.Length:
                    pNode.SetType(SmallTypeCache.Int);
                    break;

                case UnaryExpressionOperator.Negative:
                case UnaryExpressionOperator.PreDecrement:
                case UnaryExpressionOperator.PreIncrement:
                case UnaryExpressionOperator.PostDecrement:
                case UnaryExpressionOperator.PostIncrement:
                    TrySetImplicitCastType(pNode.Value, pNode.Value.Type);
                    pNode.SetType(pNode.Value.Type);
                    break;
            }
        }

        protected override void VisitBlockSyntax(BlockSyntax pNode)
        {
            _locals.AddScope();

            base.VisitBlockSyntax(pNode);

            _locals.RemoveScope();
        }

        protected override void VisitMethodSyntax(MethodSyntax pNode)
        {
            _locals.AddScope();
            _methodReturns = Utils.SyntaxHelper.SelectNodeTypes(pNode.ReturnValues);
            base.VisitMethodSyntax(pNode);
            _locals.RemoveScope();
        }

        protected override void VisitCastDefinitionSyntax(CastDefinitionSyntax pNode)
        {
            _locals.AddScope();
            _methodReturns = Utils.SyntaxHelper.SelectNodeTypes(pNode.ReturnValues);
            base.VisitCastDefinitionSyntax(pNode);
            _locals.RemoveScope();
        }

        protected override void VisitForSyntax(ForSyntax pNode)
        {
            _locals.AddScope();
            base.VisitForSyntax(pNode);
            _locals.RemoveScope();
        }

        protected override void VisitMemberAccessSyntax(MemberAccessSyntax pNode)
        {
            Visit((dynamic)pNode.Identifier);
            
            //Save current local definitions
            //Mark the current type we are on so error messages can be more descriptive
            var l = _locals;
            var t = _currentType;
            var s = _currentStruct;

            //Create a new one for the struct fields
            _currentType = pNode.Identifier.Type;
            _currentStruct = _currentType;

            //If field doesn't exist or something went wrong, stop checking things to reduce redundant errors
            if (_currentType != null && _currentType != SmallTypeCache.Undefined)
            {
                //For methods and arrays we need to allow existing variables, but member access should only allow the struct's fields
                if (pNode.Value is MethodCallSyntax || pNode.Value is ArrayAccessSyntax) _locals = _locals.Copy();
                else _locals = new VariableCache<SmallType>();

                if(!(pNode.Value is MethodCallSyntax))
                {
                    _locals.AddScope();
                    foreach (var f in _currentType.GetFields())
                    {
                        if (!_locals.IsVariableDefinedInScope(f.Name))
                        {
                            _locals.DefineVariableInScope(f.Name, f.Type);
                        }
                    }
                }

                Visit((dynamic)pNode.Value);
            }

            //Restore local definitions
            _locals = l;
            _currentType = t;
            _currentStruct = s;
        }

        protected override void VisitSelfSyntax(SelfSyntax pNode)
        {
            pNode.SetType(_currentStruct);
            base.VisitSelfSyntax(pNode);
        }

        protected override void VisitTypedIdentifierSyntax(TypedIdentifierSyntax pNode)
        {
            if (_locals.IsVariableDefinedInScope(pNode.Value))
            {
                _error.WriteError("The name '" + pNode.Value + "' is already defined in this scope", pNode.Span);
            }
            else
            {
                _locals.DefineVariableInScope(pNode.Value, pNode.Type);
            }
        }

        protected override void VisitIdentifierSyntax(IdentifierSyntax pNode)
        {
            if (!SmallTypeCache.IsTypeDefined(pNode.Value))
            {
                //Normal identifier, continue as usual
                if (!IsVariableDefined(pNode.Value, out SmallType type))
                {
                    if (_currentStruct == null) _error.WriteError("The name '" + pNode.Value + "' does not exist in the current context", pNode.Span);
                    else _error.WriteError("Type " + _currentStruct.ToString() + " does not contain a definition for '" + pNode.Value + "'", pNode.Span);
                }
                else
                {
                    pNode.SetType(type);
                }
            }
            else
            {
                //Shared or enum value
                var t = SmallTypeCache.FromString(pNode.Value);
                pNode.SetType(t);
            }
        }

        protected override void VisitArrayAccessSyntax(ArrayAccessSyntax pNode)
        {
            if (!IsVariableDefined(pNode.Value, out SmallType type))
            {
                _error.WriteError("The name '" + pNode.Value + "' does not exist in the current context", pNode.Span);
            }
            else
            {
                pNode.SetType(type);
            }
            TrySetImplicitCastType(pNode.Index, SmallTypeCache.Int);
            base.VisitArrayAccessSyntax(pNode);
        }

        protected override void VisitIfSyntax(IfSyntax pNode)
        {
            TrySetImplicitCastType(pNode.Condition, SmallTypeCache.Boolean);
            base.VisitIfSyntax(pNode);
        }

        protected override void VisitWhileSyntax(WhileSyntax pNode)
        {
            TrySetImplicitCastType(pNode.Condition, SmallTypeCache.Boolean);
            base.VisitWhileSyntax(pNode);
        }

        protected override void VisitMethodCallSyntax(MethodCallSyntax pNode)
        {
            base.VisitMethodCallSyntax(pNode);

            SmallType[] types = Utils.SyntaxHelper.SelectNodeTypes(pNode.Arguments);
            if (Utils.SyntaxHelper.HasUndefinedCastAsArg(pNode))
            {
                IList<MethodDefinition> matches = MethodCache.GetAllMatches(pNode.Value, pNode.Arguments.Count);
                if(matches.Count > 1)
                {
                    //If multiple matches are found the implicit cast could map to either method, so we can't tell
                    _error.WriteError("Cannot infer type of implicit cast. Try using an explicit cast instead.", pNode.Span);
                }
                else if(matches.Count == 1)
                {
                    //Check if we can determine implicit cast type yet
                    for (int j = 0; j < Math.Min(matches[0].ArgumentTypes.Count, pNode.Arguments.Count); j++)
                    {
                        if (Utils.SyntaxHelper.IsUndefinedCast(pNode.Arguments[j]))
                        {
                            TrySetImplicitCastType(pNode.Arguments[j], matches[0].ArgumentTypes[j]);
                        }
                    }
                }
            }

            //Check to ensure this method exists
            if (!FindMethod(out MethodDefinition m, pNode.Value, _currentType, types))
            {
                if (_currentStruct == null) _error.WriteError("Method definition for " + pNode.Value + " not found", pNode.Span);
                else _error.WriteError("Type " + _currentStruct.ToString() + " does not contain method '" + pNode.Value + "'", pNode.Span);
                return;
            }

            if (pNode.Arguments.Count != m.ArgumentTypes.Count)
            {
                _error.WriteError($"Method {pNode.Value} is expecting {m.ArgumentTypes.Count.ToString()} argument(s) but has {pNode.Arguments.Count.ToString()}", pNode.Span);
                return;
            }

            for(int i = 0; i < m.ArgumentTypes.Count; i++)
            {
                ForceCastLiteral(m.ArgumentTypes[i], pNode.Arguments[i]);
            }
            pNode.SetType(m.ReturnType);
        }

        //Fast select statements can only be generated if there are no It identifiers
        SmallType _itType;
        protected override void VisitSelectSyntax(SelectSyntax pNode)
        {
            Visit((dynamic)pNode.Condition);
            _itType = pNode.Condition.Type;

            foreach(var c in pNode.Cases)
            {
                Visit(c);
            }
        }

        protected override void VisitItSyntax(ItSyntax pNode)
        {
            pNode.SetType(_itType);
        }

        protected override void VisitReturnSyntax(ReturnSyntax pNode)
        {
            for(int i = 0; i < Math.Min(pNode.Values.Count, _methodReturns.Length); i++)
            {
                ForceCastLiteral(_methodReturns[i], pNode.Values[i]);
                //Try to cast each individual value to the corresponding return value
                TrySetImplicitCastType(pNode.Values[i], _methodReturns[i]);
            }

            base.VisitReturnSyntax(pNode);
        }

        private void ForceCastLiteral(SmallType pType, SyntaxNode pRight)
        {
            if (pRight.GetType() == typeof(NumericLiteralSyntax))
            {
                var n = (NumericLiteralSyntax)pRight;

                if (pType == SmallTypeCache.Float)
                {
                    n.NumberType = NumberTypes.Float;
                }
                else if (pType == SmallTypeCache.Double)
                {
                    n.NumberType = NumberTypes.Double;
                }
                else if (pType == SmallTypeCache.Short)
                {
                    n.NumberType = NumberTypes.Short;
                }
                else if (pType == SmallTypeCache.Int)
                {
                    n.NumberType = NumberTypes.Integer;
                }
                else if (pType == SmallTypeCache.Long)
                {
                    n.NumberType = NumberTypes.Long;
                }
            }
        }

        private void TrySetImplicitCastType(SyntaxNode pNode, SmallType pType)
        {
            if(pNode.GetType() == typeof(CastSyntax))
            {
                var c = (CastSyntax)pNode;
                if(c.Type == SmallTypeCache.Undefined)
                {
                    c.SetType(pType);
                }
            }
        }

        private bool IsVariableDefined(string pName, out SmallType pType)
        {
            if(!_locals.IsVariableDefined(pName))
            {
                if(_currentType != null)
                {
                    foreach(var trait in _currentType.Implements)
                    {
                        var i = trait.GetFieldIndex(pName);
                        if (i > -1)
                        {
                            pType = trait.GetFieldType(i);
                            return true;
                        }
                    }
                }

                pType = null;
                return false;
            }

            pType = _locals.GetVariable(pName);
            return true;
        }

        private bool FindMethod(out MethodDefinition pDef, string pName, SmallType pType, params SmallType[] pArguments)
        {
            MethodCache.FindMethod(out pDef, pType, pName, pArguments);
            if(pDef.Name == null)
            {
                if(pType != null)
                {
                    foreach (var trait in pType.Implements)
                    {
                        MethodCache.FindMethod(out pDef, trait, pName, pArguments);
                        if (pDef.Name != null) return true;
                    }
                }
                
                return false;
            }

            return true;
        }
    }
}
