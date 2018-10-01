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
        public ModuleSyntax VisitModule(ModuleSyntax pNode)
        {
            return (ModuleSyntax)Visit(pNode);
        }

        public SyntaxNode Visit(SyntaxNode pNode)
        {
            if (pNode == null) return null;

            SyntaxNode node;
            switch (pNode.SyntaxType)
            {
                case SyntaxType.It:
                    node = VisitItSyntax((ItSyntax)pNode);
                    break;

                case SyntaxType.ArrayLiteral:
                    node = VisitArrayLiteralSyntax((ArrayLiteralSyntax)pNode);
                    break;

                case SyntaxType.ArrayAccess:
                    node = VisitArrayAccessSyntax((ArrayAccessSyntax)pNode);
                    break;

                case SyntaxType.Assignment:
                    node = VisitAssignmentSyntax((AssignmentSyntax)pNode);
                    break;

                case SyntaxType.BinaryExpression:
                    node = VisitBinaryExpressionSyntax((BinaryExpressionSyntax)pNode);
                    break;

                case SyntaxType.Block:
                    node = VisitBlockSyntax((BlockSyntax)pNode);
                    break;

                case SyntaxType.BooleanLiteral:
                    node = VisitBooleanLiteralSyntax((BooleanLiteralSyntax)pNode);
                    break;

                case SyntaxType.Case:
                    node = VisitCaseSyntax((CaseSyntax)pNode);
                    break;

                case SyntaxType.CastDefinition:
                    node = VisitCastDefinitionSyntax((CastDefinitionSyntax)pNode);
                    break;

                case SyntaxType.Cast:
                    node = VisitCastSyntax((CastSyntax)pNode);
                    break;

                case SyntaxType.Discard:
                    node = VisitDiscardSyntax((DiscardSyntax)pNode);
                    break;

                case SyntaxType.NumericLiteral:
                    node = VisitNumericLiteralSyntax((NumericLiteralSyntax)pNode);
                    break;

                case SyntaxType.StringLiteral:
                    node = VisitStringLiteralSyntax((StringLiteralSyntax)pNode);
                    break;

                case SyntaxType.Self:
                    node = VisitSelfSyntax((SelfSyntax)pNode);
                    break;

                case SyntaxType.Declaration:
                    node = VisitDeclarationSyntax((DeclarationSyntax)pNode);
                    break;

                case SyntaxType.TypeDefinition:
                    node = VisitTypeDefinitionSyntax((TypeDefinitionSyntax)pNode);
                    break;

                case SyntaxType.Else:
                    node = VisitElseSyntax((ElseSyntax)pNode);
                    break;

                case SyntaxType.Enum:
                    node = VisitEnumSyntax((EnumSyntax)pNode);
                    break;

                case SyntaxType.For:
                    node = VisitForSyntax((ForSyntax)pNode);
                    break;

                case SyntaxType.MethodCall:
                    node = VisitMethodCallSyntax((MethodCallSyntax)pNode);
                    break;

                case SyntaxType.MemberAccess:
                    node = VisitMemberAccessSyntax((MemberAccessSyntax)pNode);
                    break;

                case SyntaxType.Identifier:
                    node = VisitIdentifierSyntax((IdentifierSyntax)pNode);
                    break;

                case SyntaxType.If:
                    node = VisitIfSyntax((IfSyntax)pNode);
                    break;

                case SyntaxType.Method:
                    node = VisitMethodSyntax((MethodSyntax)pNode);
                    break;

                case SyntaxType.Module:
                    node = VisitModuleSyntax((ModuleSyntax)pNode);
                    break;

                case SyntaxType.TypedIdentifier:
                    node = VisitTypedIdentifierSyntax((TypedIdentifierSyntax)pNode);
                    break;

                case SyntaxType.Return:
                    node = VisitReturnSyntax((ReturnSyntax)pNode);
                    break;

                case SyntaxType.Select:
                    node = VisitSelectSyntax((SelectSyntax)pNode);
                    break;

                case SyntaxType.StructInitializer:
                    node = VisitStructInitializerSyntax((StructInitializerSyntax)pNode);
                    break;

                case SyntaxType.Type:
                    node = VisitTypeSyntax((TypeSyntax)pNode);
                    break;

                case SyntaxType.UnaryExpression:
                    node = VisitUnaryExpressionSyntax((UnaryExpressionSyntax)pNode);
                    break;

                case SyntaxType.While:
                    node = VisitWhileSyntax((WhileSyntax)pNode);
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
            List<SyntaxNode> conditions = new List<SyntaxNode>(pNode.Conditions.Count);
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

            List<SyntaxNode> finalizer = new List<SyntaxNode>(pNode.Finalizer.Count);
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
            List<SyntaxNode> arguments = new List<SyntaxNode>(pNode.Arguments.Count);
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
            List<SyntaxNode> values = new List<SyntaxNode>(pNode.Values.Count);
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

            List<SyntaxNode> arguments = new List<SyntaxNode>(pNode.Arguments.Count);
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
