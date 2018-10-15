using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Emitting;

namespace SmallerLang.Syntax
{
    public class BlockSyntax : SyntaxNode
    {
        public IList<SyntaxNode> Statements { get; private set; }

        public override SmallType Type => SmallTypeCache.Undefined;

        public override SyntaxType SyntaxType => SyntaxType.Block;

        public BlockSyntax(IList<SyntaxNode> pStatements)
        {
            Statements = pStatements;
        }

        public override LLVMSharp.LLVMValueRef Emit(EmittingContext pContext)
        {
            pContext.Locals.AddScope();
            pContext.AddDebugScope(Span);

            foreach(var s in Statements)
            {
                if (!s.Deferred) s.Emit(pContext);
                else pContext.AddDeferredStatement(s);
            }

            BuildCallToDispose(pContext);

            pContext.RemoveDebugScope();
            pContext.Locals.RemoveScope();
            return default;
        }

        static SmallType _disposeType;
        internal static void BuildCallToDispose(EmittingContext pContext)
        {
            _disposeType = SmallTypeCache.Disposable;
            if(_disposeType != SmallTypeCache.Undefined)
            {
                foreach (var v in pContext.Locals.GetVariablesInScope())
                {
                    if (v.Type.IsAssignableFrom(_disposeType))
                    {
                        if (MethodCache.FindMethod(out MethodDefinition pDef, v.Type, "Dispose", new SmallType[] { }))
                        {
                            var func = pContext.GetMethod(pDef.MangledName);
                            LLVMSharp.LLVM.BuildCall(pContext.Builder, func, new LLVMSharp.LLVMValueRef[] { v.Value }, "");
                        }
                    }
                }
            }
        }
    }
}
