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
        readonly ConsoleErrorReporter _error;

        public SmallCompiler()
        {
            _error = new ConsoleErrorReporter();
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

            _error.SetSource(source);

            var lexer = new SmallerLexer(_error);
            var stream = lexer.StartTokenStream(source);
            var parser = new SmallerParser(stream, _error);

            var tree = parser.Parse();

            //Basic transformations that can be done without type information
            tree = new TreeRewriter(_error).VisitModule(tree);
            if (_error.ErrorOccurred) return false;

            //Info gathering passes
            new PreTypeValidation(_error).Visit(tree);
            if(_error.ErrorOccurred) return false;

            new TypeDiscoveryVisitor(_error).Visit(tree);
            if (_error.ErrorOccurred) return false;

            //Type inference
            new TypeInferenceVisitor(_error).Visit(tree);
            if (_error.ErrorOccurred) return false;

            //More advanced transformations that require type information
            tree = new PostTypeRewriter(_error).VisitModule(tree);
            if (_error.ErrorOccurred) return false;

            //Validation passes
            new TypeChecker(_error).Visit(tree);
            if (_error.ErrorOccurred) return false;

            new PostTypeValidationVisitor(_error).Visit(tree);
            if (_error.ErrorOccurred) return false;

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

            using (var c = new EmittingContext(module, passManager))
            {
                tree.Emit(c);

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
            if(!System.IO.File.Exists(pFile)) _error.WriteError($"File '{pFile}' not found");

            string source;
            try
            {
                source = System.IO.File.ReadAllText(pFile);
            }
            catch (Exception)
            {
                _error.WriteError($"Unable to read file '{pFile}'");
                return null;
            }
            return source;
        }
    }
}
