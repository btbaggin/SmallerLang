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

        protected override void VisitMethodSyntax(MethodSyntax pNode)
        {
            if(pNode.External)
            {
                if (string.IsNullOrEmpty(pNode.Annotation.Value)) _error.WriteError("External methods must be annotated with where to locate the method", pNode.Span);
                else Utils.KeyAnnotations.ValidateExternalAnnotation(pNode.Annotation, pNode, _error);
            }
        }

        protected override void VisitTypeDefinitionSyntax(TypeDefinitionSyntax pNode)
        {
            if(pNode.DefinitionType != DefinitionTypes.Implement)
            {
                var name = pNode.Name;
                if (!string.IsNullOrEmpty(SmallTypeCache.GetNamespace(ref name)))
                {
                    _error.WriteError("Struct types cannot specify namespaces", pNode.Span);
                }
            }
            base.VisitTypeDefinitionSyntax(pNode);
        }
    }
}
