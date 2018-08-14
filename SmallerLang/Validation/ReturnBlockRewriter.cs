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
        readonly IErrorReporter _error;

        public TreeRewriter(IErrorReporter pError)
        {
            _error = pError;
        }

        protected override SyntaxNode VisitBlockSyntax(BlockSyntax pNode)
        {
            //Rewrite any statements after the return statement to be a NOP
            List<SyntaxNode> statements = new List<SyntaxNode>(pNode.Statements.Count);
            bool returnFound = false;
            for (int i = 0; i < pNode.Statements.Count; i++)
            {
                if (!returnFound) statements.Add(Visit((dynamic)pNode.Statements[i]));
                else _error.WriteWarning("Unreachable code detected", pNode.Statements[i].Span);

                if (pNode.Statements[i].GetType() == typeof(ReturnSyntax))
                {
                    returnFound = true;
                }
            }

            return SyntaxFactory.Block(statements);
        }
    }
}
