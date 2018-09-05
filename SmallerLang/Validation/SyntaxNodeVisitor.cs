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
        public void Visit(SyntaxNode pNode)
        {
            if (pNode == null) return;

            switch(pNode)
            {
                case ItSyntax i:
                    VisitItSyntax(i);
                    break;

                case ArrayLiteralSyntax a:
                    VisitArrayLiteralSyntax(a);
                    break;

                case ArrayAccessSyntax a:
                    VisitArrayAccessSyntax(a);
                    break;

                case AssignmentSyntax a:
                    VisitAssignmentSyntax(a);
                    break;

                case BinaryExpressionSyntax b:
                    VisitBinaryExpressionSyntax(b);
                    break;

                case BlockSyntax b:
                    VisitBlockSyntax(b);
                    break;

                case BooleanLiteralSyntax b:
                    VisitBooleanLiteralSyntax(b);
                    break;

                case CaseSyntax c:
                    VisitCaseSyntax(c);
                    break;

                case CastDefinitionSyntax c:
                    VisitCastDefinitionSyntax(c);
                    break;

                case CastSyntax c:
                    VisitCastSyntax(c);
                    break;

                case DiscardSyntax d:
                    VisitDiscardSyntax(d);
                    break;

                case NumericLiteralSyntax n:
                    VisitNumericLiteralSyntax(n);
                    break;

                case StringLiteralSyntax s:
                    VisitStringLiteralSyntax(s);
                    break;

                case SelfSyntax s:
                    VisitSelfSyntax(s);
                    break;

                case DeclarationSyntax d:
                    VisitDeclarationSyntax(d);
                    break;

                case StructSyntax d:
                    VisitStructSyntax(d);
                    break;

                case ElseSyntax e:
                    VisitElseSyntax(e);
                    break;

                case EnumSyntax e:
                    VisitEnumSyntax(e);
                    break;

                case ForSyntax f:
                    VisitForSyntax(f);
                    break;

                case MethodCallSyntax m:
                    VisitMethodCallSyntax(m);
                    break;

                case MemberAccessSyntax m:
                    VisitMemberAccessSyntax(m);
                    break;

                case IdentifierSyntax i:
                    VisitIdentifierSyntax(i);
                    break;

                case IfSyntax i:
                    VisitIfSyntax(i);
                    break;

                case MethodSyntax m:
                    VisitMethodSyntax(m);
                    break;

                case ModuleSyntax m:
                    VisitModuleSyntax(m);
                    break;

                case TypedIdentifierSyntax p:
                    VisitTypedIdentifierSyntax(p);
                    break;

                case ReturnSyntax r:
                    VisitReturnSyntax(r);
                    break;

                case SelectSyntax s:
                    VisitSelectSyntax(s);
                    break;

                case StructInitializerSyntax s:
                    VisitStructInitializerSyntax(s);
                    break;

                case TypeSyntax t:
                    VisitTypeSyntax(t);
                    break;

                case UnaryExpressionSyntax u:
                    VisitUnaryExpressionSyntax(u);
                    break;

                case WhileSyntax w:
                    VisitWhileSyntax(w);
                    break;

                default:
                    throw new ArgumentException("pNode not of any supported type");
            }
        }

        protected virtual void VisitArrayLiteralSyntax(ArrayLiteralSyntax pNode) { }

        protected virtual void VisitArrayAccessSyntax(ArrayAccessSyntax pNode)
        {
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

        protected virtual void VisitStructSyntax(StructSyntax pNode)
        {
            for(int i = 0; i < pNode.Fields.Count; i++)
            {
                Visit(pNode.Fields[i]);
            }

            foreach(var m in pNode.Methods)
            {
                Visit(m);
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
            foreach (var a in pNode.Arguments)
            {
                Visit((dynamic)a);
            }
        }

        protected virtual void VisitMemberAccessSyntax(MemberAccessSyntax pNode)
        {
            Visit((dynamic)pNode.Identifier);
            Visit((dynamic)pNode.Value);
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
    }
}
