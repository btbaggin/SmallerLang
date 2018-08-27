using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;

namespace SmallerLang.Validation
{
    class PreTypeValidation : SyntaxNodeVisitor
    {
        readonly IErrorReporter _error;
        public PreTypeValidation(IErrorReporter pError)
        {
            _error = pError;
        }

        protected override void VisitAssignmentSyntax(AssignmentSyntax pNode)
        {
            foreach (var v in pNode.Variables)
            {
                if (!IsIdentifier(v, true))
                {
                    _error.WriteError("Subject of assignment must be an identifier", v.Span);
                }
            }
            base.VisitAssignmentSyntax(pNode);
        }

        protected override void VisitDeclarationSyntax(DeclarationSyntax pNode)
        {
            foreach (var v in pNode.Variables)
            {
                if (!IsIdentifier(v, false))
                {
                    _error.WriteError("Subject of declaration must be an identifier", v.Span);
                }
            }
            base.VisitDeclarationSyntax(pNode);
        }

        private static bool IsIdentifier(SyntaxNode pNode, bool pAdvanced)
        {
            var t = pNode.GetType();
            return t == typeof(IdentifierSyntax) ||
                   t == typeof(DiscardSyntax) ||
                   t == typeof(ItSyntax) ||
                   (pAdvanced && (t == typeof(MemberAccessSyntax) || 
                                  t == typeof(ArrayAccessSyntax)));
        }
    }
}
