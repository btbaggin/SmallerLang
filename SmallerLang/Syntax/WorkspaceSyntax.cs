using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using SmallerLang.Emitting;

namespace SmallerLang.Syntax
{
    public class WorkspaceSyntax : SyntaxNode
    {
        public override SmallType Type => SmallTypeCache.Undefined;

        public string Name { get; private set; }

        public IList<ModuleSyntax> Modules { get; private set; }

        internal WorkspaceSyntax(string pName, IList<ModuleSyntax> pModules)
        {
            Name = pName;
            Modules = pModules;
        }

        public override LLVMValueRef Emit(EmittingContext pContext)
        {
            for(int i = 0; i < Modules.Count - 1; i++)
            {
                Modules[i].Emit(pContext);
            }

            LLVMValueRef _main = GetMainModule().Emit(pContext);


            //Emit our function that the runtime will call. 
            //This will just call the method marked with "@run"
            //The reason we do this is so we have a static method name we can call
            var main = pContext.EmitMethodHeader("_main", LLVMTypeRef.Int32Type(), new LLVMTypeRef[] { });
            var mainB = main.AppendBasicBlock("");
            LLVM.PositionBuilderAtEnd(pContext.Builder, mainB);
            LLVM.BuildCall(pContext.Builder, _main, new LLVMValueRef[] { }, "");
            LLVM.BuildRet(pContext.Builder, pContext.GetInt(0));
            pContext.ValidateMethod(main);
            return default;
        }

        public ModuleSyntax GetMainModule()
        {
            return Modules[Modules.Count - 1];
        }

        public LLVMModuleRef CreateModule()
        {
            return LLVM.ModuleCreateWithName(Name);
        }
    }
}
