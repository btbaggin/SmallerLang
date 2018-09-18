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

        internal SmallType CurrentStruct { get; set; }

        internal MemberAccessStack AccessStack { get; set; }

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
            AccessStack = new MemberAccessStack();
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
                for (int i = 0; i < types.Length; i++)
                {
                    types[i] = SmallTypeCache.GetLLVMType(pMethod.ReturnValues[i].Type);
                }
                ret = LLVM.StructType(types, false);
                SmallTypeCache.SetLLVMType(pMethod.Type.Name, ret);
            }

            //If we are emitting a struct method we need to add "self" as a parameter
            SmallType[] originalTypes = new SmallType[pMethod.Parameters.Count];
            LLVMTypeRef[] parmTypes = null;
            int start = 0;
            if (CurrentStruct != null)
            {
                parmTypes = new LLVMTypeRef[pMethod.Parameters.Count + 1];
                parmTypes[0] = LLVMTypeRef.PointerType(SmallTypeCache.GetLLVMType(CurrentStruct), 0);
                start = 1;
            }
            else
            {
                parmTypes = new LLVMTypeRef[pMethod.Parameters.Count];
            }

            //Get parameter types
            for (int i = 0; i < pMethod.Parameters.Count; i++)
            {
                parmTypes[start + i] = SmallTypeCache.GetLLVMType(pMethod.Parameters[i].Type);
                if (pMethod.Parameters[i].Type.IsStruct || pMethod.Parameters[i].Type.IsArray) parmTypes[i] = LLVMTypeRef.PointerType(parmTypes[i], 0);
                originalTypes[i] = pMethod.Parameters[i].Type;
            }

            //Do not mangle external calls so they are properly exported
            pNewName = pMethod.External ? pName : MethodCache.GetMangledName(pName, CurrentStruct, originalTypes);

            //Method header
            var func = LLVM.AddFunction(CurrentModule, pNewName, LLVM.FunctionType(ret, parmTypes, false));
            LLVM.SetLinkage(func, LLVMLinkage.LLVMExternalLinkage);

            return func;
        }

        internal LLVMValueRef EmitMethodHeader(string pName, LLVMTypeRef pReturn, LLVMTypeRef[] pParms)
        {
            //Should only be called to create _main function and struct constructors
            var func = LLVM.AddFunction(CurrentModule, pName, LLVM.FunctionType(pReturn, pParms, false));
            LLVM.SetLinkage(func, LLVMLinkage.LLVMExternalLinkage);

            return func;
        }

        internal LLVMValueRef StartMethod(string pName, Syntax.MethodSyntax pNode)
        {
            var func = LLVM.GetNamedFunction(CurrentModule, pName);
            Debug.Assert(func.Pointer != IntPtr.Zero);
            Locals.AddScope();

            //Emit body
            var body = LLVM.AppendBasicBlock(func, pName + "body");
            LLVM.PositionBuilderAtEnd(Builder, body);

            int start = 0;
            if(CurrentStruct != null)
            {
                start = 1;
                LLVMValueRef p = LLVM.GetParam(func, 0);
                LLVM.SetValueName(p, "self");
                Locals.DefineParameter("self", p);
            }

            //Set parameter names and define in scope
            for (int i = 0; i < pNode.Parameters.Count; i++)
            {
                string name = pNode.Parameters[i].Value;
                LLVMValueRef parm = LLVM.GetParam(func, (uint)(i + start));
                LLVM.SetValueName(parm, name);

                Debug.Assert(!Locals.IsVariableDefinedInScope(name), $"Parameter {name} already defined");
                Locals.DefineParameter(name, parm);
            }

            CurrentMethod = func;
            return func;
        }

        public void FinishMethod(LLVMValueRef pFunction)
        {
            Locals.RemoveScope();
            ValidateMethod(pFunction);
        }

        internal LLVMValueRef GetMethod(string pName)
        {
            var func = LLVM.GetNamedFunction(CurrentModule, pName);
            Debug.Assert(func.Pointer != IntPtr.Zero);
            return func;
        }

        internal void ValidateMethod(LLVMValueRef pFunction)
        {
            if (LLVM.VerifyFunction(pFunction, LLVMVerifierFailureAction.LLVMPrintMessageAction).Value != 0)
            {
                LLVM.DumpValue(pFunction);
            }
            LLVM.RunFunctionPassManager(_passManager, pFunction);
        }
        #endregion

        #region Definition functionality
        public void EmitDefinition(string pName, Syntax.TypeDefinitionSyntax pNode)
        {
            //Get field types
            var fields = SmallTypeCache.FromString(pName).GetFields();

            LLVMTypeRef[] types = new LLVMTypeRef[fields.Length];
            for(int i = 0; i < types.Length; i++)
            {
                types[i] = SmallTypeCache.GetLLVMType(fields[i].Type);
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

            var alloc = LLVM.BuildAlloca(tempBuilder, SmallTypeCache.GetLLVMType(pType), pName);
            LLVM.DisposeBuilder(tempBuilder);
            return alloc;
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
