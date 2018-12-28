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
    class TypeInferenceVisitor : SyntaxNodeVisitor
    {
        VariableCache _locals;
        SmallType[] _methodReturns;
        readonly Compiler.CompilationCache _unit;

        public TypeInferenceVisitor(Compiler.CompilationCache pUnit)
        {
            _locals = new VariableCache();
            _unit = pUnit;
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
                using (var st = Store.AddValue("__Struct", _unit.FromString(SyntaxHelper.GetFullTypeName(s.GetApplicableType()))))
                {
                    foreach (var m in s.Methods)
                    {
                        Visit(m);
                    }
                }
            }
        }

        protected override void VisitDeclarationSyntax(DeclarationSyntax pNode)
        {
            Visit((dynamic)pNode.Value);
            var isTuple = pNode.Value.Type.IsTuple;

            for (int i = 0; i < pNode.Variables.Count; i++)
            {
                if(!SyntaxHelper.IsDiscard(pNode.Variables[i]))
                {
                    if (_locals.IsVariableDefinedInScope(pNode.Variables[i].Value))
                    {
                        CompilerErrors.IdentifierAlreadyDeclared(pNode.Variables[i], pNode.Span);
                    }
                    else
                    {
                        //We do not allow variables to have the same names as types
                        //This makes it easier to check for "static" method/fields
                        if (SmallTypeCache.IsTypeDefined(pNode.Variables[i].Value))
                        {
                            CompilerErrors.ValueDefinedAsType(pNode.Variables[i], pNode.Variables[i].Span);
                        }
                        else
                        {
                            //For tuple types we set the individual variables to the tuple field type... not the tuple itself
                            var t = isTuple ? pNode.Value.Type.GetFieldType(i) : pNode.Value.Type;

                            pNode.Variables[i].SetType(t);
                            _locals.DefineVariableInScope(pNode.Variables[i].Value, pNode.Variables[i].Type);
                        }
                    }
                }
            }

            //Check that we are declaring the proper number of variables
            if (isTuple && pNode.Value.Type.GetFieldCount() != pNode.Variables.Count)
            {
                CompilerErrors.DeclarationCountMismatch(pNode.Value.Type.GetFieldCount(), pNode.Variables.Count, pNode.Span);
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
                if (SyntaxHelper.IsDiscard(pNode.Variables[i])) ((DiscardSyntax)pNode.Variables[i]).SetType(t);
            }
        }

        protected override void VisitStructInitializerSyntax(StructInitializerSyntax pNode)
        {
            base.VisitStructInitializerSyntax(pNode);

            for (int i = 0; i < pNode.Values.Count; i++)
            {
                if(pNode.Values[i] is MemberAccessSyntax) Visit(pNode.Values[i]);
            }

            for (int i = 0; i < pNode.Arguments.Count; i++)
            {
                Visit(pNode.Arguments[i]);
            }

            var m = pNode.Type.GetConstructor();
            if(m.ArgumentTypes != null && m.ArgumentTypes.Count != pNode.Arguments.Count)
            {
                CompilerErrors.ConstructorMethodArguments(pNode.Type, m.ArgumentTypes.Count, pNode.Arguments.Count, pNode.Span);
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

        protected override void VisitMemberAccessSyntax(MemberAccessSyntax pNode)
        {
            Visit((dynamic)pNode.Identifier);
            
            //Save current local definitions
            //Mark the current type we are on so error messages can be more descriptive
            var l = _locals;

            using (var t = Store.AddValue("__Type", pNode.Identifier.Type))
            {
                //If field doesn't exist or something went wrong, stop checking things to reduce redundant errors
                if (CurrentType != SmallTypeCache.Undefined)
                {
                    //For methods and arrays we need to allow existing variables, but member access should only allow the struct's fields
                    if (pNode.Value is MethodCallSyntax || pNode.Value is ArrayAccessSyntax) _locals = _locals.Copy();
                    else
                    {
                        _locals = new VariableCache();
                        _locals.AddScope();
                        //Namespaces return a null type
                        if(CurrentType != null)
                        {
                            foreach (var f in CurrentType.GetFields())
                            {
                                if (!_locals.IsVariableDefinedInScope(f.Name))
                                {
                                    _locals.DefineVariableInScope(f.Name, f.Type);
                                }
                            }
                        }
                    }

                    Visit((dynamic)pNode.Value);
                }
            }

            //Restore local definitions
            Namespace = null;
            _locals = l;
        }

        protected override void VisitSelfSyntax(SelfSyntax pNode)
        {
            pNode.SetType(Struct);
            base.VisitSelfSyntax(pNode);
        }

        protected override void VisitTypeSyntax(TypeSyntax pNode)
        {
            foreach (var a in pNode.GenericArguments)
            {
                Visit(a);
            }

            var name = SyntaxHelper.GetFullTypeName(pNode);
            var ns = SmallTypeCache.GetNamespace(ref name);

            SmallType type;
            if (string.IsNullOrEmpty(ns)) type = _unit.FromString(name);
            else type = _unit.FromStringInNamespace(ns, name);
            if (type.IsGenericType)
            {
                type = _unit.MakeConcreteType(type, SyntaxHelper.SelectNodeTypes(pNode.GenericArguments));
            }
            pNode.SetType(type);

            if(pNode.Namespace != null && !_unit.HasReference(pNode.Namespace))
            {
                CompilerErrors.NamespaceNotDefined(pNode.Namespace, pNode.Span);
            }
        }

        protected override void VisitTypedIdentifierSyntax(TypedIdentifierSyntax pNode)
        {
            base.VisitTypedIdentifierSyntax(pNode);

            if (_locals.IsVariableDefinedInScope(pNode.Value))
            {
                CompilerErrors.IdentifierAlreadyDeclared(pNode, pNode.Span);
            }
            else
            {
                _locals.DefineVariableInScope(pNode.Value, pNode.Type);
            }
        }

        protected override void VisitIdentifierSyntax(IdentifierSyntax pNode)
        {
            if (CurrentType != null || !_unit.IsTypeDefined(pNode.Value))
            {
                //Normal identifier, continue as usual
                if (!IsVariableDefined(pNode.Value, out SmallType type))
                {
                    if (CurrentType == null)
                    {
                        //Generate a slightly different error message if we are in a struct
                        //This can happen if we forget self
                        if (Struct != null) CompilerErrors.IdentifierNotDeclaredSelf(pNode, pNode.Span);
                        else CompilerErrors.IdentifierNotDeclared(pNode, pNode.Span);
                    }
                    else CompilerErrors.IdentifierNotDeclared(CurrentType, pNode, pNode.Span);
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
            Visit((dynamic)pNode.Identifier);
            pNode.SetType(pNode.Identifier.Type);

            TrySetImplicitCastType(pNode.Index, SmallTypeCache.Int);
            Visit((dynamic)pNode.Index);
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

            SmallType[] types = SyntaxHelper.SelectNodeTypes(pNode.Arguments);
            if (SyntaxHelper.HasUndefinedCastAsArg(pNode))
            {
                IList<MethodDefinition> matches = _unit.GetAllMatches(pNode.Value, pNode.Arguments.Count);
                if(matches.Count > 1)
                {
                    //If multiple matches are found the implicit cast could map to either method, so we can't tell
                    CompilerErrors.InferImplicitCast(pNode.Span);
                }
                else if(matches.Count == 1)
                {
                    //Check if we can determine implicit cast type yet
                    for (int j = 0; j < Math.Min(matches[0].ArgumentTypes.Count, pNode.Arguments.Count); j++)
                    {
                        if (SyntaxHelper.IsUndefinedCast(pNode.Arguments[j]))
                        {
                            TrySetImplicitCastType(pNode.Arguments[j], matches[0].ArgumentTypes[j]);
                            types[j] = pNode.Arguments[j].Type;
                        }
                    }
                }
            }

            //Check to ensure this method exists
            if (!SyntaxHelper.FindMethodOnType(out MethodDefinition m, _unit, Namespace, pNode.Value, CurrentType, types))
            {
                CompilerErrors.MethodNotFound(m, Struct, pNode.Value, pNode.Arguments, pNode.Span);
                return;
            }

            for(int i = 0; i < m.ArgumentTypes.Count; i++)
            {
                ForceCastLiteral(m.ArgumentTypes[i], pNode.Arguments[i]);
            }
            
            //Poly our method definition to match any generic types
            m = m.MakeConcreteDefinition(CurrentType);
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


        protected override void VisitForSyntax(ForSyntax pNode)
        {
            _locals.AddScope();

            if (pNode.Iterator != null)
            {
                Visit((dynamic)pNode.Iterator);

                //Array vs Enumerable<T>
                if (pNode.Iterator.Type.IsArray)
                {
                    _itType = pNode.Iterator.Type.GetElementType();
                }
                else if(SmallTypeCache.TryGetEnumerable(_unit, out SmallType enumerable) && pNode.Iterator.Type.IsAssignableFrom(enumerable))
                {
                    _itType = pNode.Iterator.Type.GenericArguments[0];
                }
                else
                {
                    _itType = pNode.Iterator.Type;
                }
            }
            else
            {
                foreach (var d in pNode.Initializer)
                {
                    Visit((dynamic)d);
                }
                Visit((dynamic)pNode.Condition);

                foreach (var f in pNode.Finalizer)
                {
                    Visit((dynamic)f);
                }
            }

            Visit(pNode.Body);

            _locals.RemoveScope();
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
            if (pRight is NumericLiteralSyntax n)
            {
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
            if(pNode is CastSyntax c && c.Type == SmallTypeCache.Undefined)
            {
                c.SetType(pType);
            }
        }

        private bool IsVariableDefined(string pName, out SmallType pType)
        {
            if(!_locals.IsVariableDefined(pName))
            {
                pType = null;
                return false;
            }

            pType = _locals.GetVariable(pName).Type;
            return true;
        }
    }
}
