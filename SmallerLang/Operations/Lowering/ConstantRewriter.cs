using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;
using SmallerLang.Emitting;

namespace SmallerLang.Operations.Lowering
{
    /*
     * This will take all constants and substitute their literal values
     */
    partial class PostTypeRewriter : SyntaxNodeRewriter
    {
        ScopeCache<SyntaxNode> _locals = new ScopeCache<SyntaxNode>();
        protected override SyntaxNode VisitDeclarationSyntax(DeclarationSyntax pNode)
        {
            if(pNode.IsConst)
            {
                foreach(var v in pNode.Variables)
                {
                    _locals.DefineVariableInScope(v.Value, pNode.Value);
                }
            }
            return base.VisitDeclarationSyntax(pNode);
        }

        protected override SyntaxNode VisitIdentifierSyntax(IdentifierSyntax pNode)
        {
            //TODO this isn't 100% because we could define a var with the same name as the constant
            if(_locals.IsVariableDefined(pNode.Value))
            {
                return _locals.GetVariable(pNode.Value);
            }
            return base.VisitIdentifierSyntax(pNode);
        }
    }
}
