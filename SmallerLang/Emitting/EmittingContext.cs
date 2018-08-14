using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using LLVMSharp;

namespace SmallerLang.Emitting
{
    public sealed class EmittingContext : IDisposable
    {
        public static LLVMBool False => new LLVMBool(0);
        public static LLVMBool True => new LLVMBool(1);

        public LLVMModuleRef CurrentModule { get; private set; }
        public LLVMValueRef CurrentMethod { get; private set; }
        public LLVMBuilderRef Builder { get; private set; }

        public VariableCache<LLVMValueRef> Locals { get; private set; }

        private readonly LLVMPassManagerRef _passManager;
        private readonly LLVMContextRef _context;

        private readonly Stack<List<Syntax.SyntaxNode>> _deferredStatements;

        internal EmittingContext(LLVMModuleRef pModule, LLVMPassManagerRef pPass)
        {
            CurrentModule = pModule;
            _passManager = pPass;
            _context = LLVM.GetGlobalContext();
            _deferredStatements = new Stack<List<Syntax.SyntaxNode>>();
            Builder = LLVM.CreateBuilder();
            Locals = new VariableCache<LLVMValueRef>();
        }

        #region Method functionality
        public LLVMValueRef EmitMethodHeader(string pName, Syntax.MethodSyntax pMethod, out string pNewName)
        {
            //Get method return type
            LLVMTypeRef ret;
            if (pMethod.ReturnValues.Count == 0) ret = LLVMTypeRef.VoidType();
            else if (pMethod.ReturnValues.Count == 1) ret = SmallTypeCache.GetLLVMType(pMethod.Type);
            else
            {
                LLVMTypeRef[] types = new LLVMTypeRef[pMethod.ReturnValues.Count];
                for(int i = 0; i < types.Length; i++)
                {
                    types[i] = SmallTypeCache.GetLLVMType(pMethod.ReturnValues[i].Type);
                }
                ret = LLVM.StructType(types, false);
                SmallTypeCache.SetLLVMType(pMethod.Type.Name, ret);
            }

            //Get parameter types
            LLVMTypeRef[] parmTypes = new LLVMTypeRef[pMethod.Parameters.Count];
            SmallType[] originalTypes = new SmallType[pMethod.Parameters.Count];
            for (int i = 0; i < pMethod.Parameters.Count; i++)
            {
                parmTypes[i] = SmallTypeCache.GetLLVMType(pMethod.Parameters[i].Type);
                if (pMethod.Parameters[i].Type.IsStruct || pMethod.Parameters[i].Type.IsArray) parmTypes[i] = LLVMTypeRef.PointerType(parmTypes[i], 0);
                originalTypes[i] = pMethod.Parameters[i].Type;
            }

            //Do not mangle external calls so they are properly exported
            pNewName = pMethod.External ? pName : MethodCache.GetMangledName(pName, originalTypes);

            //Method header
            var f = LLVM.AddFunction(CurrentModule, pNewName, LLVM.FunctionType(ret, parmTypes, false));
            LLVM.SetLinkage(f, LLVMLinkage.LLVMExternalLinkage);

            return f;
        }

        internal LLVMValueRef EmitMethodHeader(string pName, LLVMTypeRef pReturn, LLVMTypeRef[] pParms)
        {
            //Should only be called to create _main function and struct constructors
            var f = LLVM.AddFunction(CurrentModule, pName, LLVM.FunctionType(pReturn, pParms, false));
            LLVM.SetLinkage(f, LLVMLinkage.LLVMExternalLinkage);

            return f;
        }

        internal LLVMValueRef StartMethod(string pName, Syntax.MethodSyntax pNode)
        {
            var f = LLVM.GetNamedFunction(CurrentModule, pName);
            Debug.Assert(f.Pointer != IntPtr.Zero);
            Locals.AddScope();

            //Emit body
            var b = LLVM.AppendBasicBlock(f, pName + "body");
            LLVM.PositionBuilderAtEnd(Builder, b);

            //Set parameter names and define in scope
            for (int i = 0; i < pNode.Parameters.Count; i++)
            {
                string name = pNode.Parameters[i].Value;
                LLVMValueRef p = LLVM.GetParam(f, (uint)i);
                LLVM.SetValueName(p, name);

                Debug.Assert(!Locals.IsVariableDefinedInScope(name), $"Parameter {name} already defined");
                Locals.DefineParameter(name, p);
            }

            CurrentMethod = f;
            return f;
        }

        public void FinishMethod(LLVMValueRef f, bool pScope = true)
        {
            if(pScope) Locals.RemoveScope();
            if (LLVM.VerifyFunction(f, LLVMVerifierFailureAction.LLVMPrintMessageAction).Value != 0)
            {
                LLVM.DumpValue(f);
            }
            LLVM.RunFunctionPassManager(_passManager, f);
        }

        internal LLVMValueRef GetMethod(string pName)
        {
            return LLVM.GetNamedFunction(CurrentModule, pName);
        }
        #endregion

        #region Definition functionality
        public void EmitDefinition(string pName, Syntax.StructSyntax pNode)
        {
            //Get field types
            LLVMTypeRef[] types = new LLVMTypeRef[pNode.Fields.Count];
            for(int i = 0; i < types.Length; i++)
            {
                types[i] = SmallTypeCache.GetLLVMType(pNode.Fields[i].Type);
            }

            //Emit struct
            var t = LLVM.StructCreateNamed(_context, pName);
            t.StructSetBody(types, false);
            SmallTypeCache.SetLLVMType(pName, t);
        }
        #endregion

        public LLVMValueRef AllocateVariable(string pName, SmallType pType)
        {
            //Move to the start of the current function and emit the variable allocation
            var tempBuilder = GetTempBuilder();
            LLVM.PositionBuilder(tempBuilder, CurrentMethod.GetEntryBasicBlock(), CurrentMethod.GetEntryBasicBlock().GetFirstInstruction());
            var a = LLVM.BuildAlloca(tempBuilder, SmallTypeCache.GetLLVMType(pType), pName);
            LLVM.DisposeBuilder(tempBuilder);
            return a;
        }

        public LLVMBuilderRef GetTempBuilder()
        {
            return LLVM.CreateBuilder();
        }

        internal void SetBuilder(LLVMBuilderRef pBuilder)
        {
            Builder = pBuilder;
        }

        #region Deferred statements
        public void AddDeferredStatementExecution()
        {
            _deferredStatements.Push(new List<Syntax.SyntaxNode>());
        }

        public void AddDeferredStatement(Syntax.SyntaxNode pNode)
        {
            Debug.Assert(_deferredStatements.Count > 0);
            _deferredStatements.Peek().Add(pNode);
        }

        public IEnumerable<Syntax.SyntaxNode> GetDeferredStatements()
        {
            return _deferredStatements.Peek();
        }

        public void RemoveDeferredStatementExecution()
        {
            Debug.Assert(_deferredStatements.Count > 0);
            _deferredStatements.Pop();
        }
        #endregion

        #region Constants
        public LLVMValueRef GetInt1(int pValue)
        {
            return LLVM.ConstInt(LLVM.Int1Type(), (ulong)pValue, False);
        }
        public LLVMValueRef GetShort(short pShort)
        {
            return LLVM.ConstInt(LLVM.Int16Type(), (ulong)pShort, False);
        }

        public LLVMValueRef GetInt(int pInt)
        {
            return LLVM.ConstInt(LLVM.Int32Type(), (ulong)pInt, False);
        }

        public LLVMValueRef GetLong(long pLong)
        {
            return LLVM.ConstInt(LLVM.Int64Type(), (ulong)pLong, False);
        }

        public LLVMValueRef GetFloat(float pFloat)
        {
            return LLVM.ConstReal(LLVM.FloatType(), pFloat);
        }

        public LLVMValueRef GetDouble(double pDouble)
        {
            return LLVM.ConstReal(LLVM.DoubleType(), pDouble);
        }

        public LLVMValueRef GetString(string pString)
        {
            return  LLVM.ConstString(pString, (uint)pString.Length, false);
        }

        public LLVMValueRef GetArray(SmallType pType, int pSize)
        {
            LLVMValueRef[] values = new LLVMValueRef[pSize];
            LLVMValueRef def = SmallTypeCache.GetLLVMDefault(pType, this);
            for (int i = 0; i < pSize; i++)
            {
                values[i] = def;
            }
            var t = pType.IsArray ? pType.GetElementType() : pType;
            return LLVM.ConstArray(SmallTypeCache.GetLLVMType(t), values);
        }
        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls
        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing) { /* No managed resources to free*/ }

                LLVM.DisposeBuilder(Builder);
                Locals = null;
                disposedValue = true;
            }
        }

        ~EmittingContext()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
