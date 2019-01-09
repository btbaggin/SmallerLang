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
using SmallerLang.Utils;
using LLVMSharp;

namespace SmallerLang.Compiler
{
    public class SmallCompiler
    {
        public static string CurrentDirectory { get; private set; }

        public SmallCompiler()
        {
            CompilerErrors.SetReporter(new ConsoleErrorReporter());
        }

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
            pModule = null;
            string source = string.IsNullOrEmpty(pOptions.SourceFile) ? pOptions.Source : ReadSourceFile(pOptions.SourceFile);
            if (source == null) return false;

            var lexer = new SmallerLexer();
            var stream = lexer.StartTokenStream(source, pOptions.SourceFile);
            var parser = new SmallerParser(stream);

            var tree = parser.Parse();
            if (CompilerErrors.ErrorOccurred) return false;

            var compilationModule = ModuleBuilder.Build(tree);
            if (compilationModule == null) return false;

            LLVMModuleRef module = LLVM.ModuleCreateWithName(tree.Name);
            LLVMPassManagerRef passManager = LLVM.CreateFunctionPassManagerForModule(module);

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

            using (var c = new EmittingContext(module, passManager, pOptions.Debug))
            {
                compilationModule.Emit(c);

                if (LLVM.VerifyModule(module, LLVMVerifierFailureAction.LLVMPrintMessageAction, out string message).Value != 0)
                {
                    LLVM.DumpModule(module);
                    LLVM.DisposePassManager(passManager);
                    pModule = null;
                    return false;
                }
            }
                
            pModule = module;
            LLVM.DisposePassManager(passManager);
            return true;
        }

        private string ReadSourceFile(string pFile)
        {
            if(!System.IO.File.Exists(pFile)) CompilerErrors.FileNotFound(pFile);

            string source;
            try
            {
                source = System.IO.File.ReadAllText(pFile);
            }
            catch (Exception)
            {
                CompilerErrors.UnableToReadFile(pFile);
                return null;
            }

            CurrentDirectory = System.IO.Path.GetDirectoryName(pFile);
            return source;
        }
    }
}
