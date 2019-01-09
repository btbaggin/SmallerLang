using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;
using SmallerLang.Lowering;
using SmallerLang.Utils;
using SmallerLang.Validation;
using LLVMSharp;

namespace SmallerLang.Compiler
{
    public class CompilationModule
    {
        public ModuleSyntax Module { get; private set; }
        public CompilationCache Cache { get; private set; }

        public CompilationModule(ModuleSyntax pModule, string pNamespace)
        {
            Cache = new CompilationCache(pNamespace);
            Module = pModule;
        }

        public bool Compile(CompilationCache pCompilation)
        {
            Module = new TreeRewriter(pCompilation).VisitModule(Module);
            if (CompilerErrors.ErrorOccurred) return false;

            //Info gathering passes
            new PreTypeValidation().Visit(Module);
            if (CompilerErrors.ErrorOccurred) return false;

            new TypeDiscoveryVisitor(pCompilation).Visit(Module);
            if (CompilerErrors.ErrorOccurred) return false;

            //Type inference
            new TypeInferenceVisitor(pCompilation).Visit(Module);
            if (CompilerErrors.ErrorOccurred) return false;

            //TODO need to poly referenced methods

            //More advanced transformations that require type information
            Module = new PostTypeRewriter(pCompilation).VisitModule(Module);
            if (CompilerErrors.ErrorOccurred) return false;

            //Validation passes
            new TypeChecker(pCompilation).Visit(Module);
            if (CompilerErrors.ErrorOccurred) return false;

            new PostTypeValidationVisitor(pCompilation).Visit(Module);
            if (CompilerErrors.ErrorOccurred) return false;

            new PolyRewriter(pCompilation).Visit(Module);
            if (CompilerErrors.ErrorOccurred) return false;

            return true;
        }

        public void Emit(Emitting.EmittingContext pContext)
        {
            EmitReferencedNodes(pContext);

            pContext.Cache = Cache;
            LLVMValueRef _main = Module.Emit(pContext);

            //Emit our function that the runtime will call. 
            //This will just call the method marked with "@run"
            //The reason we do this is so we have a static method name we can call
            var main = pContext.EmitMethodHeader("_main", LLVMTypeRef.Int32Type(), new LLVMTypeRef[] { });
            var mainB = main.AppendBasicBlock("");
            LLVM.PositionBuilderAtEnd(pContext.Builder, mainB);
            LLVM.BuildCall(pContext.Builder, _main, new LLVMValueRef[] { }, "");
            LLVM.BuildRet(pContext.Builder, pContext.GetInt(0));
            pContext.ValidateMethod(main);
        }

        private void EmitReferencedNodes(Emitting.EmittingContext pContext)
        {
            var mrv = new ModuleReferenceVisitor(Cache, pContext);
            mrv.Visit(Module);

            //Emit types. Need to do it in order of dependencies so all types resolve
            foreach (var i in mrv.TypeNodes.OrderBy((pS) => ((TypeDefinitionSyntax)pS.Node).EmitOrder))
            {
                pContext.Cache = i.Unit;
                i.Node.Emit(pContext);
            }

            //Emit type methods headers
            foreach (var m in mrv.MethodNodes)
            {
                pContext.Cache = m.Unit;
                ((MethodSyntax)m.Node).EmitHeader(pContext);
            }
            foreach (var s in mrv.TypeNodes)
            {
                pContext.Cache = s.Unit;
                ((TypeDefinitionSyntax)s.Node).EmitMethodHeaders(pContext);
            }

            //Emit type methods
            foreach (var m in mrv.MethodNodes)
            {
                pContext.Cache = m.Unit;
                ((MethodSyntax)m.Node).Emit(pContext);
            }
            foreach (var s in mrv.TypeNodes)
            {
                pContext.Cache = s.Unit;
                ((TypeDefinitionSyntax)s.Node).EmitMethods(pContext);
            }
        }
    }
}
