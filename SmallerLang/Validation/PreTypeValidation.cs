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
        protected override void VisitMethodSyntax(MethodSyntax pNode)
        {
            if(pNode.External)
            {
                KeyAnnotations.ValidateExternalAnnotation(pNode.Annotation, pNode);
            }
        }

        protected override void VisitTypeDefinitionSyntax(TypeDefinitionSyntax pNode)
        {
            if(pNode.DefinitionType != DefinitionTypes.Implement)
            {
                var name = pNode.Name;
                if (!string.IsNullOrEmpty(SmallTypeCache.GetNamespace(ref name)))
                {
                    CompilerErrors.StructNamespace(pNode.Span);
                }
            }
            base.VisitTypeDefinitionSyntax(pNode);
        }
    }
}
