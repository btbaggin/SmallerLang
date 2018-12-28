using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;
using SmallerLang.Utils;

namespace SmallerLang.Lowering
{
    partial class TreeRewriter : SyntaxNodeRewriter
    {
        private Compiler.CompilationCache _unit;
        public TreeRewriter(Compiler.CompilationCache pUnit)
        {
            _unit = pUnit;
        }

        protected override SyntaxNode VisitBlockSyntax(BlockSyntax pNode)
        {
            //Rewrite any statements after the return statement to be a NOP
            List<SyntaxNode> statements = new List<SyntaxNode>(pNode.Statements.Count);
            bool returnFound = false;
            for (int i = 0; i < pNode.Statements.Count; i++)
            {
                if (!returnFound) statements.Add(Visit((dynamic)pNode.Statements[i]));
                else CompilerErrors.UnreachableCode(pNode.Statements[i].Span);

                if (pNode.Statements[i].SyntaxType == SyntaxType.Return)
                {
                    returnFound = true;
                }
            }

            return SyntaxFactory.Block(statements);
        }
    }
}
