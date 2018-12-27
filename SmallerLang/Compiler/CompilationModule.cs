﻿using System;
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
        ModuleSyntax _module;
        public CompilationUnit Unit { get; private set; }

        public CompilationModule(ModuleSyntax pModule)
        {
            Unit = new CompilationUnit();
            _module = pModule;
        }

        public bool Compile(CompilationUnit pCompilation)
        {
            _module = new TreeRewriter(pCompilation).VisitModule(_module);
            if (CompilerErrors.ErrorOccurred) return false;

            //Info gathering passes
            new PreTypeValidation().Visit(_module);
            if (CompilerErrors.ErrorOccurred) return false;

            new TypeDiscoveryVisitor(pCompilation).Visit(_module);
            if (CompilerErrors.ErrorOccurred) return false;

            //Type inference
            new TypeInferenceVisitor(pCompilation).Visit(_module);
            if (CompilerErrors.ErrorOccurred) return false;

            //TODO need to poly referenced nodes and types

            //More advanced transformations that require type information
            _module = new PostTypeRewriter(pCompilation).VisitModule(_module);
            if (CompilerErrors.ErrorOccurred) return false;

            //Validation passes
            new TypeChecker(pCompilation).Visit(_module);
            if (CompilerErrors.ErrorOccurred) return false;

            new PostTypeValidationVisitor().Visit(_module);
            if (CompilerErrors.ErrorOccurred) return false;

            new PolyRewriter().Visit(_module);
            if (CompilerErrors.ErrorOccurred) return false;

            return true;
        }

        public void Emit(Emitting.EmittingContext pContext)
        {
            EmitReferencedNodes(pContext);

            pContext.Unit = Unit;
            LLVMValueRef _main = _module.Emit(pContext);

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
            //TODO this isn't right because i need to bring the nodes all the way to the top
            if (Unit.ReferenceCount() > 0)
            {
                foreach (var r in Unit.GetReferences())
                {
                    r.Module.EmitReferencedNodes(pContext);

                    var mrv = new ModuleReferenceVisitor(r.Alias, r.Module._module);
                    mrv.Visit(_module);

                    pContext.Unit = r.Module.Unit;
                    //Emit types. Need to do it in order of dependencies so all types resolve
                    foreach (var i in mrv.UsedTypes.OrderBy((pS) => pS.EmitOrder))
                    {
                        i.Emit(pContext);
                    }

                    //Emit type methods headers
                    foreach (var m in mrv.UsedMethods)
                    {
                        m.EmitHeader(pContext);
                    }
                    foreach(var s in mrv.UsedTypes)
                    {
                        s.EmitMethodHeaders(pContext);
                    }

                    //Emit type methods
                    foreach(var s in mrv.UsedTypes)
                    {
                        s.EmitMethods(pContext);
                    }
                    foreach (var m in mrv.UsedMethods)
                    {
                        m.Emit(pContext);
                    }
                }
            }
        }
    }
}