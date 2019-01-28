using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;
using SmallerLang.Utils;

namespace SmallerLang.Operations.Lowering
{
    /*
     * This class will ensure that there are no statements after a return
     * Statements after a return are invalid in LLVM
     * We can also provide warnings that the code is unreachable
     */
    partial class PreTypeRewriter : SyntaxNodeRewriter
    {
        private Compiler.CompilationCache _unit;
        public PreTypeRewriter(Compiler.CompilationCache pUnit)
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
                if (!returnFound) statements.Add(Visit(pNode.Statements[i]));
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
