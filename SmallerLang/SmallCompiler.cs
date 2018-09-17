using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Emitting;
using SmallerLang.Lexer;
using SmallerLang.Parser;
using SmallerLang.Validation;
using SmallerLang.Lowering;
using SmallerLang.Syntax;
using LLVMSharp;

namespace SmallerLang
{
    public class SmallCompiler
    {
        public bool Compile(CompilerOptions pOptions)
        {
            if(Compile(pOptions, out LLVMModuleRef? m) && !string.IsNullOrEmpty(pOptions.OutputFile))
            {
                LLVM.WriteBitcodeToFile(m.Value, pOptions.OutputFile);
                if (pOptions.OutputBytecode) LLVM.DumpModule(m.Value);
                LLVM.DisposeModule(m.Value);
                return true;
            }

            return false;
        }

        public bool Compile(CompilerOptions pOptions, out LLVMModuleRef? pModule)
        {
            string source = string.IsNullOrEmpty(pOptions.SourceFile) ? pOptions.Source : System.IO.File.ReadAllText(pOptions.SourceFile);

            IErrorReporter reporter = new ConsoleErrorReporter(source);
            var lexer = new SmallerLexer(reporter);
            var stream = lexer.StartTokenStream(source);
            var parser = new SmallerParser(stream, reporter);

            pModule = null;
            var t = parser.Parse();
            t = (ModuleSyntax)new TreeRewriter(reporter).Visit(t);

            new PreTypeValidation(reporter).Visit(t);
            if(reporter.ErrorOccurred) return false;

            new TypeDiscoveryVisitor(reporter).Visit(t);
            //Info gathering passes
            new TypeInferenceVisitor(reporter).Visit(t);
            if (reporter.ErrorOccurred) return false;

            //Validation passes
            new TypeChecker(reporter).Visit(t);
            if (reporter.ErrorOccurred) return false;

            new PostTypeValidationVisitor(reporter).Visit(t);
            if (reporter.ErrorOccurred) return false;

            LLVMModuleRef m = LLVM.ModuleCreateWithName(t.Name);
            LLVMPassManagerRef passManager = LLVM.CreateFunctionPassManagerForModule(m);

            if(pOptions.Optimizations)
            {
                //Promote allocas to registers
                LLVM.AddPromoteMemoryToRegisterPass(passManager);

                //Do simple peephole optimizations
                LLVM.AddInstructionCombiningPass(passManager);

                //Re-associate expressions
                LLVM.AddReassociatePass(passManager);

                //Eliminate common subexpressions
                LLVM.AddGVNPass(passManager);

                //Simplify control flow graph
                LLVM.AddCFGSimplificationPass(passManager);
            }
            LLVM.InitializeFunctionPassManager(passManager);

            using (var c = new EmittingContext(m, passManager))
            {
                t.Emit(c);

                if (LLVM.VerifyModule(m, LLVMVerifierFailureAction.LLVMPrintMessageAction, out string message).Value != 0)
                {
                    LLVM.DumpModule(m);
                    LLVM.DisposePassManager(passManager);
                    pModule = null;
                    return false;
                }
            }
                
            pModule = m;
            LLVM.DisposePassManager(passManager);
            return true;
        }
    }
}
