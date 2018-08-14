using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using SmallerLang.Syntax;

namespace SmallerLang.Utils
{
    static class SyntaxHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LastStatementIsReturn(BlockSyntax pBlock)
        {
            return pBlock.Statements[pBlock.Statements.Count - 1].GetType() == typeof(ReturnSyntax);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LastStatementIsReturn(ElseSyntax pElse)
        {
            if (pElse.Body == null) return false;
            return LastStatementIsReturn(pElse.Body);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasUndefinedCastAsArg(MethodCallSyntax pMethod)
        {
            for(int i = 0; i < pMethod.Arguments.Count; i++)
            {
                if (pMethod.Arguments[i].Type == SmallTypeCache.Undefined &&
                   pMethod.Arguments[i].GetType() == typeof(CastSyntax))
                {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsUndefinedCast(SyntaxNode pNode)
        {
            return pNode?.Type == SmallTypeCache.Undefined && pNode?.GetType() == typeof(CastSyntax);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCastDefinition(SyntaxNode pNode)
        {
            return pNode?.GetType() == typeof(CastDefinitionSyntax);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDiscard(SyntaxNode pNode)
        {
            return pNode.GetType() == typeof(DiscardSyntax);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SelectIsComplete(SelectSyntax pNode)
        {
            return pNode.Annotation == "complete";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SmallType[] SelectNodeTypes<T>(IList<T> pNodes) where T : SyntaxNode
        {
            SmallType[] types = new SmallType[pNodes.Count];
            for(int i = 0; i < types.Length; i++)
            {
                types[i] = pNodes[i].Type;
            }
            return types;
        }
    }
}
