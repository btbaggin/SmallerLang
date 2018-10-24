using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Emitting;
using System.Diagnostics;

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

        internal static void BuildCallToDispose(EmittingContext pContext)
        {
            if(NamespaceManager.TryGetStdLib(out NamespaceContainer container))
            {
                SmallType Disposable = container.FindType("Disposable");
                Debug.Assert(Disposable != SmallTypeCache.Undefined, "stdlib does not define Disposable");

                foreach (var v in pContext.Locals.GetVariablesInScope())
                {
                    if (v.Type.IsAssignableFrom(Disposable))
                    {
                        Debug.Assert(MethodCache.FindMethod(out MethodDefinition pDef, pContext.CurrentNamespace, v.Type, "Dispose", new SmallType[] { }), "Disposable does not implement Dispose");

                        var func = pContext.GetMethod(pDef.MangledName);
                        LLVMSharp.LLVM.BuildCall(pContext.Builder, func, new LLVMSharp.LLVMValueRef[] { v.Value }, "");
                }
                }
            }
                
        }
    }
}
