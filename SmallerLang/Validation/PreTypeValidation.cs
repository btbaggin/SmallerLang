using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;
using SmallerLang.Utils;

namespace SmallerLang.Validation
{
    class PreTypeValidation : SyntaxNodeVisitor
    {
        protected override void VisitTypeDefinitionSyntax(TypeDefinitionSyntax pNode)
        {
            if(pNode.DefinitionType != DefinitionTypes.Implement && pNode.DeclaredType.Namespace != null)
            {
                 CompilerErrors.StructNamespace(pNode.Span);
            }
            base.VisitTypeDefinitionSyntax(pNode);
        }
    }
}
