using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;

namespace SmallerLang.Validation
{
    public abstract class SyntaxNodeVisitor
    {
        protected string Namespace { get; set;}
        protected SmallType Type
        {
            get { return Store.GetValueOrDefault<SmallType>("__Type"); }
        }

        protected SmallType Struct
        {
            get { return Store.GetValueOrDefault<SmallType>("__Struct"); }
        }

        protected readonly VisitorStore Store;

        protected SyntaxNodeVisitor()
        {
            Store = new VisitorStore();
            Namespace = "";
        }

        public void Visit(SyntaxNode pNode)
        {
            if (pNode == null) return;

            switch(pNode.SyntaxType)
            {
                case SyntaxType.It:
                    VisitItSyntax((ItSyntax)pNode);
                    break;

                case SyntaxType.ArrayLiteral:
                    VisitArrayLiteralSyntax((ArrayLiteralSyntax)pNode);
                    break;

                case SyntaxType.ArrayAccess:
                    VisitArrayAccessSyntax((ArrayAccessSyntax)pNode);
                    break;

                case SyntaxType.Assignment:
                    VisitAssignmentSyntax((AssignmentSyntax)pNode);
                    break;

                case SyntaxType.BinaryExpression:
                    VisitBinaryExpressionSyntax((BinaryExpressionSyntax)pNode);
                    break;

                case SyntaxType.Block:
                    VisitBlockSyntax((BlockSyntax)pNode);
                    break;

                case SyntaxType.BooleanLiteral:
                    VisitBooleanLiteralSyntax((BooleanLiteralSyntax)pNode);
                    break;

                case SyntaxType.Break:
                    VisitBreakSyntax((BreakSyntax)pNode);
                    break;

                case SyntaxType.Case:
                    VisitCaseSyntax((CaseSyntax)pNode);
                    break;

                case SyntaxType.CastDefinition:
                    VisitCastDefinitionSyntax((CastDefinitionSyntax)pNode);
                    break;

                case SyntaxType.Cast:
                    VisitCastSyntax((CastSyntax)pNode);
                    break;

                case SyntaxType.Discard:
                    VisitDiscardSyntax((DiscardSyntax)pNode);
                    break;

                case SyntaxType.Namespace:
                    VisitNamespaceSyntax((NamespaceSyntax)pNode);
                    break;

                case SyntaxType.NumericLiteral:
                    VisitNumericLiteralSyntax((NumericLiteralSyntax)pNode);
                    break;

                case SyntaxType.StringLiteral:
                    VisitStringLiteralSyntax((StringLiteralSyntax)pNode);
                    break;

                case SyntaxType.Self:
                    VisitSelfSyntax((SelfSyntax)pNode);
                    break;

                case SyntaxType.Declaration:
                    VisitDeclarationSyntax((DeclarationSyntax)pNode);
                    break;

                case SyntaxType.TypeDefinition:
                    VisitTypeDefinitionSyntax((TypeDefinitionSyntax)pNode);
                    break;

                case SyntaxType.Else:
                    VisitElseSyntax((ElseSyntax)pNode);
                    break;

                case SyntaxType.Enum:
                    VisitEnumSyntax((EnumSyntax)pNode);
                    break;

                case SyntaxType.For:
                    VisitForSyntax((ForSyntax)pNode);
                    break;

                case SyntaxType.MethodCall:
                    VisitMethodCallSyntax((MethodCallSyntax)pNode);
                    break;

                case SyntaxType.MemberAccess:
                    VisitMemberAccessSyntax((MemberAccessSyntax)pNode);
                    break;

                case SyntaxType.Identifier:
                    VisitIdentifierSyntax((IdentifierSyntax)pNode);
                    break;

                case SyntaxType.If:
                    VisitIfSyntax((IfSyntax)pNode);
                    break;

                case SyntaxType.Method:
                    VisitMethodSyntax((MethodSyntax)pNode);
                    break;

                case SyntaxType.Module:
                    VisitModuleSyntax((ModuleSyntax)pNode);
                    break;

                case SyntaxType.TypedIdentifier:
                    VisitTypedIdentifierSyntax((TypedIdentifierSyntax)pNode);
                    break;

                case SyntaxType.Return:
                    VisitReturnSyntax((ReturnSyntax)pNode);
                    break;

                case SyntaxType.Select:
                    VisitSelectSyntax((SelectSyntax)pNode);
                    break;

                case SyntaxType.StructInitializer:
                    VisitStructInitializerSyntax((StructInitializerSyntax)pNode);
                    break;

                case SyntaxType.Type:
                    VisitTypeSyntax((TypeSyntax)pNode);
                    break;

                case SyntaxType.UnaryExpression:
                    VisitUnaryExpressionSyntax((UnaryExpressionSyntax)pNode);
                    break;

                case SyntaxType.While:
                    VisitWhileSyntax((WhileSyntax)pNode);
                    break;

                case SyntaxType.Workspace:
                    VisitWorkspaceSyntax((WorkspaceSyntax)pNode);
                    break;

                default:
                    throw new ArgumentException("pNode not of any supported type");
            }
        }

        protected virtual void VisitArrayLiteralSyntax(ArrayLiteralSyntax pNode) { }

        protected virtual void VisitArrayAccessSyntax(ArrayAccessSyntax pNode)
        {
            Visit((dynamic)pNode.Identifier);
            Visit((dynamic)pNode.Index);
        }

        protected virtual void VisitAssignmentSyntax(AssignmentSyntax pNode)
        {
            foreach(var v in pNode.Variables)
            {
                Visit((dynamic)v);
            }
            Visit((dynamic)pNode.Value);
        }

        protected virtual void VisitBinaryExpressionSyntax(BinaryExpressionSyntax pNode)
        {
            Visit((dynamic)pNode.Left);
            Visit((dynamic)pNode.Right);
        }

        protected virtual void VisitBlockSyntax(BlockSyntax pNode)
        {
            foreach(var s in pNode.Statements)
            {
                Visit((dynamic)s);
            }
        }

        protected virtual void VisitBooleanLiteralSyntax(BooleanLiteralSyntax pNode) { }

        protected virtual void VisitBreakSyntax(BreakSyntax pNode) { }

        protected virtual void VisitCaseSyntax(CaseSyntax pNode)
        {
            foreach(var c in pNode.Conditions)
            {
                Visit((dynamic)c);
            }
            Visit(pNode.Body);
        }

        protected virtual void VisitCastDefinitionSyntax(CastDefinitionSyntax pNode)
        {
            Visit(pNode.Parameters[0]);
            Visit(pNode.ReturnValues[0]);
            Visit(pNode.Body);
        }

        protected virtual void VisitCastSyntax(CastSyntax pNode)
        {
            Visit((dynamic)pNode.Value);
        }

        protected virtual void VisitDiscardSyntax(DiscardSyntax pNode) { }

        protected virtual void VisitDeclarationSyntax(DeclarationSyntax pNode)
        {
            foreach (var v in pNode.Variables)
            {
                Visit((dynamic)v);
            }
            Visit((dynamic)pNode.Value);
        }

        protected virtual void VisitTypeDefinitionSyntax(TypeDefinitionSyntax pNode)
        {
            using (var s = Store.AddValue("__Struct", pNode.GetApplicableType()))
            {
                for (int i = 0; i < pNode.Fields.Count; i++)
                {
                    Visit(pNode.Fields[i]);
                }

                foreach (var m in pNode.Methods)
                {
                    Visit(m);
                }
            }
        }

        protected virtual void VisitElseSyntax(ElseSyntax pNode)
        {
            Visit(pNode.If);
            Visit(pNode.Body);
        }

        protected virtual void VisitEnumSyntax(EnumSyntax pNode)
        {
            foreach(var v in pNode.Names)
            {
                Visit(v);
            }
        }

        protected virtual void VisitForSyntax(ForSyntax pNode)
        {
            if(pNode.Iterator != null)
            {
                Visit((dynamic)pNode.Iterator);
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
        }

        protected virtual void VisitIdentifierSyntax(IdentifierSyntax pNode) { }

        protected virtual void VisitIfSyntax(IfSyntax pNode)
        {
            Visit((dynamic)pNode.Condition);
            Visit(pNode.Body);
            Visit(pNode.Else);
        }

        protected virtual void VisitItSyntax(ItSyntax pNode) { }

        protected virtual void VisitMethodSyntax(MethodSyntax pNode)
        {
            foreach(var p in pNode.Parameters)
            {
                Visit((dynamic)p);
            }
            foreach(var r in pNode.ReturnValues)
            {
                Visit(r);
            }
            Visit(pNode.Body);
        }

        protected virtual void VisitMethodCallSyntax(MethodCallSyntax pNode)
        {
            var n = Namespace;
            Namespace = "";

            using (var t = Store.AddValue<SmallType>("__Type", null))
            {
                foreach (var a in pNode.Arguments)
                {
                    Visit((dynamic)a);
                }
            }

            Namespace = n;
        }

        protected virtual void VisitMemberAccessSyntax(MemberAccessSyntax pNode)
        {
            Visit((dynamic)pNode.Identifier);
            using (var t = Store.AddValue("__Type", pNode.Identifier.Type))
            {
                Visit((dynamic)pNode.Value);
            }

            Namespace = "";
        }

        protected virtual void VisitModuleSyntax(ModuleSyntax pNode)
        {
            foreach(var m in pNode.Methods)
            {
                Visit(m);
            }
            foreach(var d in pNode.Structs)
            {
                Visit(d);
            }
            foreach(var e in pNode.Enums)
            {
                Visit(e);
            }
        }

        protected virtual void VisitNamespaceSyntax(NamespaceSyntax pNode)
        {
            Namespace = pNode.Value;
        }

        protected virtual void VisitNumericLiteralSyntax(NumericLiteralSyntax pNode) { }

        protected virtual void VisitTypedIdentifierSyntax(TypedIdentifierSyntax pNode) { }

        protected virtual void VisitReturnSyntax(ReturnSyntax pNode)
        {
            foreach(var v in pNode.Values)
            {
                Visit((dynamic)v);
            }
        }

        protected virtual void VisitSelectSyntax(SelectSyntax pNode)
        {
            Visit((dynamic)pNode.Condition);
            foreach(var c in pNode.Cases)
            {
                Visit(c);
            }
        }

        protected virtual void VisitSelfSyntax(SelfSyntax pNode) { }

        protected virtual void VisitStructInitializerSyntax(StructInitializerSyntax pNode)
        {
            Visit(pNode.Struct);
            foreach(var a in pNode.Arguments)
            {
                Visit((dynamic)a);
            }
        }

        protected virtual void VisitStringLiteralSyntax(StringLiteralSyntax pNode) { }

        protected virtual void VisitTypeSyntax(TypeSyntax pNode) { }

        protected virtual void VisitUnaryExpressionSyntax(UnaryExpressionSyntax pNode)
        {
            Visit((dynamic)pNode.Value);
        }

        protected virtual void VisitWhileSyntax(WhileSyntax pNode)
        {
            Visit((dynamic)pNode.Condition);
            Visit(pNode.Body);
        }

        protected virtual void VisitWorkspaceSyntax(WorkspaceSyntax pNode)
        {
            foreach(var m in pNode.Imports)
            {
                Visit(m.Value);
            }
            Visit(pNode.Module);
        }
    }
}
