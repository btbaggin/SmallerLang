using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;

namespace SmallerLang.Operations.Lowering
{
    partial class PostTypeRewriter : SyntaxNodeRewriter
    {
        Emitting.ScopeCache<SyntaxNode> _locals = new Emitting.ScopeCache<SyntaxNode>();
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
            if(_locals.IsVariableDefined(pNode.Value))
            {
                return _locals.GetVariable(pNode.Value);
            }
            return base.VisitIdentifierSyntax(pNode);
        }

        private void FindConstants(ModuleSyntax pModule)
        {
            foreach (var f in pModule.Fields)
            {
                Visit(f);
            }
        }
    }
}
