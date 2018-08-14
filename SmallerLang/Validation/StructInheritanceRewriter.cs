using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;

namespace SmallerLang.Validation
{
    partial class TreeRewriter : SyntaxNodeRewriter
    {
        ModuleSyntax _module;
        protected override SyntaxNode VisitModuleSyntax(ModuleSyntax pNode)
        {
            _module = pNode;
            return base.VisitModuleSyntax(pNode);
        }

        protected override SyntaxNode VisitStructSyntax(StructSyntax pNode)
        {
            //Add any base fields to the inherited stuct
            if(!string.IsNullOrEmpty(pNode.Inherits))
            {
                List<TypedIdentifierSyntax> fields = new List<TypedIdentifierSyntax>();
                List<ExpressionSyntax> defaults = new List<ExpressionSyntax>();
                for(int i = 0; i < _module.Definitions.Count; i++)
                {
                    if(_module.Definitions[i].Name == pNode.Inherits)
                    {
                        fields.AddRange(_module.Definitions[i].Fields);
                        defaults.AddRange(_module.Definitions[i].Defaults);
                        break;
                    }
                }
                fields.AddRange(pNode.Fields);
                defaults.AddRange(pNode.Defaults);
                return SyntaxFactory.Struct(pNode.Name, pNode.Inherits, fields, defaults, pNode.TypeParameters);
            }
            return base.VisitStructSyntax(pNode);
        }
    }
}
