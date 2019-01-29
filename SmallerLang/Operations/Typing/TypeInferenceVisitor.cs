using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;
using SmallerLang.Emitting;
using SmallerLang.Utils;

namespace SmallerLang.Operations.Typing
{
    class TypeInferenceVisitor : SyntaxNodeVisitor
    {
        ScopeCache<LocalDefinition> _locals;
        SmallType[] _methodReturns;
        readonly Compiler.CompilationCache _unit;

        public TypeInferenceVisitor(Compiler.CompilationCache pUnit)
        {
            _locals = new ScopeCache<LocalDefinition>();
            _unit = pUnit;
        }

        protected override void VisitModuleSyntax(ModuleSyntax pNode)
        {
            //Global scope
            _locals.AddScope();

            foreach(var f in pNode.Fields)
            {
                Visit(f);
            }

            //Infer methods
            foreach (var m in pNode.Methods)
            {
                Visit(m);
            }

            foreach (var s in pNode.Structs)
            {
                Visit(s.DeclaredType);

                _unit.FromString(s.GetApplicableType(), out SmallType type);
                using (var st = Store.AddValue("__Struct", type))
                {
                    foreach (var m in s.Methods)
                    {
                        Visit(m);
                    }
                }
            }

            _locals.RemoveScope();
        }

        protected override void VisitDeclarationSyntax(DeclarationSyntax pNode)
        {
            Visit(pNode.Value);

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

                            //Report expression errors and change the type to Undefined so we don't get further no expression errors
                            if (pNode.Value.Type == SmallTypeCache.NoValue)
                            {
                                CompilerErrors.ExpressionNoValue(pNode.Value.Span);
                                t = SmallTypeCache.Undefined;
                            }

                            pNode.Variables[i].SetType(t);
                            _locals.DefineVariableInScope(pNode.Variables[i].Value, LocalDefinition.Create(pNode.IsConst, pNode.Variables[i].Type));
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
            if(pNode.Value.Type == SmallTypeCache.NoValue)
            {
                CompilerErrors.ExpressionNoValue(pNode.Value.Span);
            }

            var isTuple = pNode.Value.Type.IsTuple;
            for (int i = 0; i < pNode.Variables.Count; i++)
            {
                //Check if we are assigning to a const variable
                if(_locals.TryGetVariable(pNode.Variables[i].Value, out LocalDefinition ld) && ld.IsConst)
                {
                    CompilerErrors.CannotAssignCost(pNode.Variables[i], pNode.Variables[i].Span);
                }

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

                case BinaryExpressionOperator.BitwiseAnd:
                case BinaryExpressionOperator.BitwiseOr:
                    pNode.SetType(pNode.Left.Type);
                    break;

                case BinaryExpressionOperator.LeftBitShift:
                case BinaryExpressionOperator.RightBitShift:
                    pNode.SetType(SmallTypeCache.Int);
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
            _methodReturns = SyntaxHelper.SelectNodeTypes(pNode.ReturnValues);
            base.VisitMethodSyntax(pNode);
            _locals.RemoveScope();
        }

        protected override void VisitCastDefinitionSyntax(CastDefinitionSyntax pNode)
        {
            _locals.AddScope();
            _methodReturns = SyntaxHelper.SelectNodeTypes(pNode.ReturnValues);
            base.VisitCastDefinitionSyntax(pNode);
            _locals.RemoveScope();
        }

        protected override void VisitMemberAccessSyntax(MemberAccessSyntax pNode)
        {
            Visit(pNode.Identifier);
            
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
                        _locals = new ScopeCache<LocalDefinition>();
                        _locals.AddScope();
                        //Namespaces return a null type
                        if(CurrentType != null)
                        {
                            foreach (var f in CurrentType.GetFields())
                            {
                                if (!_locals.IsVariableDefinedInScope(f.Name))
                                {
                                    _locals.DefineVariableInScope(f.Name, LocalDefinition.Create(false, f.Type));
                                }
                            }
                        }
                    }

                    Visit(pNode.Value);
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
            //TODO i need to have <T> after methods so we can define our generic types and propagate them
            foreach (var a in pNode.GenericArguments)
            {
                Visit(a);
            }

            var result = _unit.FromString(pNode, out SmallType type);
            switch(result)
            {
                //case Compiler.FindResult.NotFound:
                //    CompilerErrors.UndeclaredType(name, pNode.Span);
                //    break;

                case Compiler.FindResult.IncorrectScope:
                    CompilerErrors.TypeNotInScope(SyntaxHelper.GetFullTypeName(pNode), pNode.Span);
                    break;
            }

            if (type.IsGenericType)
            {
                type = _unit.MakeConcreteType(type, SyntaxHelper.SelectNodeTypes(pNode.GenericArguments));
            }
            pNode.SetType(type);

            if(pNode.Namespace != null && !_unit.HasReference(pNode.Namespace.Value))
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
                _locals.DefineVariableInScope(pNode.Value, LocalDefinition.Create(false, pNode.Type));
            }
        }

        protected override void VisitIdentifierSyntax(IdentifierSyntax pNode)
        {
            if (CurrentType != null || !_unit.IsTypeDefined(Namespace, pNode.Value))
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
                var result = _unit.FromString(Namespace, pNode.Value, out SmallType t);
                switch(result)
                {
                    case Compiler.FindResult.Found:
                        pNode.SetType(t);
                        break;

                    case Compiler.FindResult.IncorrectScope:
                        CompilerErrors.TypeNotInScope(pNode.Value, pNode.Span);
                        break;

                    case Compiler.FindResult.NotFound:
                        CompilerErrors.UndeclaredType(pNode.Value, pNode.Span);
                        break;
                }
            }
        }

        protected override void VisitArrayAccessSyntax(ArrayAccessSyntax pNode)
        {
            Visit(pNode.Identifier);
            pNode.SetType(pNode.Identifier.Type);

            TrySetImplicitCastType(pNode.Index, SmallTypeCache.Int);
            Visit(pNode.Index);
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

            //TODO somehow report methods that do not return values
            SmallType[] types = new SmallType[pNode.Arguments.Count];
            for (int i = 0; i < types.Length; i++)
            {
                types[i] = pNode.Arguments[i].Type;
                if(types[i] == SmallTypeCache.NoValue)
                {
                    CompilerErrors.ExpressionNoValue(pNode.Arguments[i].Span);
                }
            }

            if (SyntaxHelper.HasUndefinedCastAsArg(pNode))
            {
                IList<MethodDefinition> matches = _unit.GetAllMatches(Namespace, pNode.Value, pNode.Arguments.Count);
                if(matches.Count > 1)
                {
                    //If multiple matches are found the implicit cast could map to either method, so we can't tell
                    CompilerErrors.InferImplicitCast(pNode.Span);
                    return;
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
            var result = SyntaxHelper.FindMethodOnType(out MethodDefinition m, _unit, Namespace, pNode.Value, CurrentType, types);
            switch(result)
            {
                case Compiler.FindResult.NotFound:
                    CompilerErrors.MethodNotFound(m, Struct, pNode.Value, pNode.Arguments, pNode.Span);
                    return;

                case Compiler.FindResult.IncorrectScope:
                    CompilerErrors.MethodNotInScope(m, Struct, pNode.Value, pNode.Arguments, pNode.Span);
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
            Visit(pNode.Condition);
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
                Visit(pNode.Iterator);

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
                    Visit(d);
                }
                Visit(pNode.Condition);

                foreach (var f in pNode.Finalizer)
                {
                    Visit(f);
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
