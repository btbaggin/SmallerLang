using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;

namespace SmallerLang.Lowering
{
    public abstract class SyntaxNodeRewriter
    {
        public SyntaxNode Visit(SyntaxNode pNode)
        {
            if (pNode == null) return null;

            SyntaxNode node;
            switch (pNode)
            {
                case ItSyntax i:
                    node = VisitItSyntax(i);
                    break;

                case ArrayLiteralSyntax a:
                    node = VisitArrayLiteralSyntax(a);
                    break;

                case ArrayAccessSyntax a:
                    node = VisitArrayAccessSyntax(a);
                    break;

                case AssignmentSyntax a:
                    node = VisitAssignmentSyntax(a);
                    break;

                case BinaryExpressionSyntax b:
                    node = VisitBinaryExpressionSyntax(b);
                    break;

                case BlockSyntax b:
                    node = VisitBlockSyntax(b);
                    break;

                case BooleanLiteralSyntax b:
                    node = VisitBooleanLiteralSyntax(b);
                    break;

                case CaseSyntax c:
                    node = VisitCaseSyntax(c);
                    break;

                case CastDefinitionSyntax c:
                    node = VisitCastDefinitionSyntax(c);
                    break;

                case CastSyntax c:
                    node = VisitCastSyntax(c);
                    break;

                case DiscardSyntax d:
                    node = VisitDiscardSyntax(d);
                    break;

                case NumericLiteralSyntax n:
                    node = VisitNumericLiteralSyntax(n);
                    break;

                case StringLiteralSyntax s:
                    node = VisitStringLiteralSyntax(s);
                    break;

                case SelfSyntax s:
                    node = VisitSelfSyntax(s);
                    break;

                case DeclarationSyntax d:
                    node = VisitDeclarationSyntax(d);
                    break;

                case TypeDefinitionSyntax d:
                    node = VisitTypeDefinitionSyntax(d);
                    break;

                case ElseSyntax e:
                    node = VisitElseSyntax(e);
                    break;

                case EnumSyntax e:
                    node = VisitEnumSyntax(e);
                    break;

                case ForSyntax f:
                    node = VisitForSyntax(f);
                    break;

                case MethodCallSyntax m:
                    node = VisitMethodCallSyntax(m);
                    break;

                case MemberAccessSyntax m:
                    node = VisitMemberAccessSyntax(m);
                    break;

                case IdentifierSyntax i:
                    node = VisitIdentifierSyntax(i);
                    break;

                case IfSyntax i:
                    node = VisitIfSyntax(i);
                    break;

                case MethodSyntax m:
                    node = VisitMethodSyntax(m);
                    break;

                case ModuleSyntax m:
                    node = VisitModuleSyntax(m);
                    break;

                case TypedIdentifierSyntax p:
                    node = VisitTypedIdentifierSyntax(p);
                    break;

                case ReturnSyntax r:
                    node = VisitReturnSyntax(r);
                    break;

                case SelectSyntax s:
                    node = VisitSelectSyntax(s);
                    break;

                case StructInitializerSyntax s:
                    node = VisitStructInitializerSyntax(s);
                    break;

                case TypeSyntax t:
                    node = VisitTypeSyntax(t);
                    break;

                case UnaryExpressionSyntax u:
                    node = VisitUnaryExpressionSyntax(u);
                    break;

                case WhileSyntax w:
                    node = VisitWhileSyntax(w);
                    break;

                default:
                    throw new ArgumentException("pNode not of any supported type");
            }

            return node.FromNode(pNode);
        }

        protected virtual SyntaxNode VisitArrayLiteralSyntax(ArrayLiteralSyntax pNode) { return pNode; }

        protected virtual SyntaxNode VisitArrayAccessSyntax(ArrayAccessSyntax pNode)
        {
            return SyntaxFactory.ArrayAccess(pNode.Identifier, Visit((dynamic)pNode.Index));
        }

        protected virtual SyntaxNode VisitAssignmentSyntax(AssignmentSyntax pNode)
        {
            List<IdentifierSyntax> variables = new List<IdentifierSyntax>(pNode.Variables.Count);
            foreach(var v in pNode.Variables)
            {
                variables.Add(Visit((dynamic)v));
            }
            return SyntaxFactory.Assignment(variables, pNode.Operator, Visit((dynamic)pNode.Value));
        }

        protected virtual SyntaxNode VisitBinaryExpressionSyntax(BinaryExpressionSyntax pNode)
        {
            return SyntaxFactory.BinaryExpression(Visit((dynamic)pNode.Left), pNode.Operator, Visit((dynamic)pNode.Right));
        }

        protected virtual SyntaxNode VisitBlockSyntax(BlockSyntax pNode)
        {
            List<SyntaxNode> statements = new List<SyntaxNode>(pNode.Statements.Count);
            foreach (var s in pNode.Statements)
            {
                statements.Add(Visit((dynamic)s));
            }
            return SyntaxFactory.Block(statements);
        }

        protected virtual SyntaxNode VisitBooleanLiteralSyntax(BooleanLiteralSyntax pNode)
        {
            return pNode;
        }

        protected virtual SyntaxNode VisitCaseSyntax(CaseSyntax pNode)
        {
            List<ExpressionSyntax> conditions = new List<ExpressionSyntax>(pNode.Conditions.Count);
            foreach(var c in pNode.Conditions)
            {
                conditions.Add(Visit((dynamic)c));
            }
            return SyntaxFactory.Case(conditions, (BlockSyntax)Visit(pNode.Body));
        }

        protected virtual SyntaxNode VisitCastDefinitionSyntax(CastDefinitionSyntax pNode)
        {
            return SyntaxFactory.CastDefinition((TypedIdentifierSyntax)Visit(pNode.Parameters[0]), (BlockSyntax)Visit(pNode.Body), (TypeSyntax)Visit(pNode.ReturnValues[0]));
        }

        protected virtual SyntaxNode VisitCastSyntax(CastSyntax pNode)
        {
            return SyntaxFactory.Cast(Visit((dynamic)pNode.Value), (TypeSyntax)Visit(pNode.TypeNode));
        }

        protected virtual SyntaxNode VisitDiscardSyntax(DiscardSyntax pNode)
        {
            return SyntaxFactory.Discard();
        }

        protected virtual SyntaxNode VisitDeclarationSyntax(DeclarationSyntax pNode)
        {
            List<IdentifierSyntax> variables = new List<IdentifierSyntax>(pNode.Variables.Count);
            foreach (var v in pNode.Variables)
            {
                variables.Add(Visit((dynamic)v));
            }
            return SyntaxFactory.Declaration(variables, Visit((dynamic)pNode.Value));
        }

        protected virtual SyntaxNode VisitTypeDefinitionSyntax(TypeDefinitionSyntax pNode)
        {
            List<TypedIdentifierSyntax> fields = new List<TypedIdentifierSyntax>(pNode.Fields.Count);
            List<MethodSyntax> methods = new List<MethodSyntax>(pNode.Methods.Count);
            for (int i = 0; i < pNode.Fields.Count; i++)
            {
                fields.Add((TypedIdentifierSyntax)Visit(pNode.Fields[i]));
            }

            foreach(var m in pNode.Methods)
            {
                methods.Add((MethodSyntax)Visit(m));
            }

            return SyntaxFactory.TypeDefinition(pNode.Name, pNode.AppliesTo, pNode.DefinitionType, methods, fields, pNode.TypeParameters);
        }

        protected virtual SyntaxNode VisitElseSyntax(ElseSyntax pNode)
        {
            return SyntaxFactory.Else((IfSyntax)Visit(pNode.If), (BlockSyntax)Visit(pNode.Body));
        }

        protected virtual SyntaxNode VisitEnumSyntax(EnumSyntax pNode)
        {
            List<IdentifierSyntax> values = new List<IdentifierSyntax>(pNode.Values.Count);
            foreach(var v in pNode.Names)
            {
                values.Add(Visit((dynamic)v));
            }
            return SyntaxFactory.Enum(pNode.Name, values, pNode.Values);
        }

        protected virtual SyntaxNode VisitForSyntax(ForSyntax pNode)
        {
            if(pNode.Iterator != null)
            {
                return SyntaxFactory.For(Visit((dynamic)pNode.Iterator), (BlockSyntax)Visit(pNode.Body));
            }

            List<DeclarationSyntax> initializer = new List<DeclarationSyntax>(pNode.Initializer.Count);
            foreach (var d in pNode.Initializer)
            {
                initializer.Add(Visit((dynamic)d));
            }
            Visit((dynamic)pNode.Condition);

            List<ExpressionSyntax> finalizer = new List<ExpressionSyntax>(pNode.Finalizer.Count);
            foreach (var f in pNode.Finalizer)
            {
                finalizer.Add(Visit((dynamic)f));
            }
            Visit(pNode.Body);

            return SyntaxFactory.For(initializer, Visit((dynamic)pNode.Condition), finalizer, (BlockSyntax)Visit(pNode.Body));
        }

        protected virtual SyntaxNode VisitIdentifierSyntax(IdentifierSyntax pNode)
        {
            return SyntaxFactory.Identifier(pNode.Value);
        }

        protected virtual SyntaxNode VisitIfSyntax(IfSyntax pNode)
        {
            return SyntaxFactory.If(Visit((dynamic)pNode.Condition), (BlockSyntax)Visit(pNode.Body), (ElseSyntax)Visit(pNode.Else));
        }

        protected virtual SyntaxNode VisitItSyntax(ItSyntax pNode)
        {
            return SyntaxFactory.It();
        }

        protected virtual SyntaxNode VisitMethodSyntax(MethodSyntax pNode)
        {
            List<TypedIdentifierSyntax> parameters = new List<TypedIdentifierSyntax>(pNode.Parameters.Count);
            foreach (var p in pNode.Parameters)
            {
                parameters.Add(Visit((dynamic)p));
            }

            List<TypeSyntax> returns = new List<TypeSyntax>(pNode.ReturnValues.Count);
            foreach(var r in pNode.ReturnValues)
            {
                returns.Add((TypeSyntax)Visit(r));
            }

            MethodSyntax m = SyntaxFactory.Method(pNode.Name, returns, parameters, (BlockSyntax)Visit(pNode.Body), pNode.External);

            m.Annotation = pNode.Annotation;
            return m;
        }

        protected virtual SyntaxNode VisitMethodCallSyntax(MethodCallSyntax pNode)
        {
            List<ExpressionSyntax> arguments = new List<ExpressionSyntax>(pNode.Arguments.Count);
            foreach (var a in pNode.Arguments)
            {
                arguments.Add(Visit((dynamic)a));
            }
            return SyntaxFactory.MethodCall(pNode.Value, arguments);
        }

        protected virtual SyntaxNode VisitMemberAccessSyntax(MemberAccessSyntax pNode)
        {
            return SyntaxFactory.MemberAccess(Visit((dynamic)pNode.Identifier), Visit((dynamic)pNode.Value));
        }

        protected virtual SyntaxNode VisitModuleSyntax(ModuleSyntax pNode)
        {
            List<MethodSyntax> methods = new List<MethodSyntax>(pNode.Methods.Count);
            foreach (var m in pNode.Methods)
            {
                methods.Add((MethodSyntax)Visit(m));
            }

            List<TypeDefinitionSyntax> definitions = new List<TypeDefinitionSyntax>(pNode.Structs.Count);
            foreach (var d in pNode.Structs)
            {
                definitions.Add(Visit((dynamic)d));
            }

            List<EnumSyntax> enums = new List<EnumSyntax>(pNode.Enums.Count);
            foreach(var e in pNode.Enums)
            {
                enums.Add((EnumSyntax)Visit(e));
            }
            return SyntaxFactory.Module(pNode.Name, methods, definitions, enums);
        }

        protected virtual SyntaxNode VisitNumericLiteralSyntax(NumericLiteralSyntax pNode)
        {
            return pNode;
        }

        protected virtual SyntaxNode VisitTypedIdentifierSyntax(TypedIdentifierSyntax pNode)
        {
            return SyntaxFactory.TypedIdentifier((TypeSyntax)Visit(pNode.TypeNode), pNode.Value);
        }

        protected virtual SyntaxNode VisitReturnSyntax(ReturnSyntax pNode)
        {
            List<ExpressionSyntax> values = new List<ExpressionSyntax>(pNode.Values.Count);
            foreach(var v in pNode.Values)
            {
                values.Add(Visit((dynamic)v));
            }
            return SyntaxFactory.Return(values);
        }

        protected virtual SyntaxNode VisitSelectSyntax(SelectSyntax pNode)
        {
            var cond = Visit((dynamic)pNode.Condition);

            List<CaseSyntax> cases = new List<CaseSyntax>(pNode.Cases.Count);
            foreach (var c in pNode.Cases)
            {
                cases.Add((CaseSyntax)Visit(c));
            }
            return SyntaxFactory.Select(cond, cases);
        }

        protected virtual SyntaxNode VisitSelfSyntax(SelfSyntax pNode)
        {
            return SyntaxFactory.Self();
        }

        protected virtual SyntaxNode VisitStructInitializerSyntax(StructInitializerSyntax pNode)
        {
            List<IdentifierSyntax> variables = new List<IdentifierSyntax>(pNode.Values.Count);
            foreach(var v in pNode.Values)
            {
                variables.Add(Visit((dynamic)v));
            }

            var t = (TypeSyntax)Visit(pNode.Struct);

            List<ExpressionSyntax> arguments = new List<ExpressionSyntax>(pNode.Arguments.Count);
            foreach(var a in pNode.Arguments)
            {
                arguments.Add(Visit((dynamic)a));
            }
            return SyntaxFactory.StructInitializer(variables, t, arguments);
        }

        protected virtual SyntaxNode VisitStringLiteralSyntax(StringLiteralSyntax pNode)
        {
            return pNode;
        }

        protected virtual SyntaxNode VisitTypeSyntax(TypeSyntax pNode)
        {
            return pNode;
        }

        protected virtual SyntaxNode VisitUnaryExpressionSyntax(UnaryExpressionSyntax pNode)
        {
            return SyntaxFactory.UnaryExpression(Visit((dynamic)pNode.Value), pNode.Operator);
        }

        protected virtual SyntaxNode VisitWhileSyntax(WhileSyntax pNode)
        {
            return SyntaxFactory.While(Visit((dynamic)pNode.Condition), (BlockSyntax)Visit(pNode.Body));
        }
    }
}
