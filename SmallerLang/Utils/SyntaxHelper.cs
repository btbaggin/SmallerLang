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
            return pBlock.Statements.Count > 0 && pBlock.Statements[pBlock.Statements.Count - 1].SyntaxType == SyntaxType.Return;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool LastStatementIsBreak(BlockSyntax pBlock)
        {
            return pBlock.Statements.Count > 0 && pBlock.Statements[pBlock.Statements.Count - 1].SyntaxType == SyntaxType.Break;
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
                   pMethod.Arguments[i].SyntaxType == SyntaxType.Cast)
                {
                    return true;
                }
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsUndefinedCast(SyntaxNode pNode)
        {
            return pNode?.Type == SmallTypeCache.Undefined && pNode?.SyntaxType == SyntaxType.Cast;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCastDefinition(SyntaxNode pNode)
        {
            return pNode?.SyntaxType == SyntaxType.CastDefinition;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsDiscard(SyntaxNode pNode)
        {
            return pNode.SyntaxType == SyntaxType.Discard;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMemberAccess(SyntaxNode pNode)
        {
            return pNode.SyntaxType == SyntaxType.MemberAccess || pNode.SyntaxType == SyntaxType.ArrayAccess;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool MustReallocateOnDeclaration(SyntaxNode pNode)
        {
            return pNode.SyntaxType == SyntaxType.ArrayLiteral;
        }
    }
}
