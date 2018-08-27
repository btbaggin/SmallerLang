﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Emitting;

namespace SmallerLang.Syntax
{
    public class MethodSyntax : SyntaxNode
    {
        public string Name { get; protected set; }

        public IList<TypedIdentifierSyntax> Parameters { get; private set; }

        public BlockSyntax Body { get; private set; }

        public IList<TypeSyntax> ReturnValues { get; private set; }

        public bool External { get; private set; }

        public override SmallType Type
        {
            get
            {
                //No return values = void
                //1 Return value = normal type
                //> 1 return values is a tuple
                if (ReturnValues.Count == 0) return SmallTypeCache.Undefined;
                if (ReturnValues.Count == 1) return ReturnValues[0].Type;
                return SmallTypeCache.GetTempTuple(Utils.SyntaxHelper.SelectNodeTypes(ReturnValues));
            }
        }

        string _name;
        internal MethodSyntax(string pName, IList<TypeSyntax> pReturns, IList<TypedIdentifierSyntax> pParameters, BlockSyntax pBody, bool pExternal)
        {
            Name = pName;
            ReturnValues = pReturns;
            Parameters = pParameters;
            Body = pBody;
            External = pExternal;
        }

        public LLVMSharp.LLVMValueRef EmitHeader(EmittingContext pContext)
        {
            //Emit header
            return pContext.EmitMethodHeader(Name, this, out _name);
        }

        public override LLVMSharp.LLVMValueRef Emit(EmittingContext pContext)
        {
            if (!External)
            {
                System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(_name), "Method name cannot be blank");
                var f = pContext.StartMethod(_name, this);
                pContext.AddDeferredStatementExecution();
                pContext.Locals.AddScope();

                //Method bodies are slightly special because we want all variables to be declared in their scope
                //Don't call Body.Emit because that starts a new scope and all our variables will be not declared for deferred statements
                foreach (var s in Body.Statements)
                {
                    if (!s.Deferred) s.Emit(pContext);
                    else pContext.AddDeferredStatement(s);
                }

                //Emit all deferred statements unless the return handled it for us
                var lastIsReturn = Utils.SyntaxHelper.LastStatementIsReturn(Body);
                if(!lastIsReturn)
                {
                    foreach (var s in pContext.GetDeferredStatements())
                    {
                        s.Emit(pContext);
                    }
                }

                if (ReturnValues.Count == 0)
                {
                    LLVMSharp.LLVM.BuildRetVoid(pContext.Builder);
                }
                else if(!lastIsReturn)
                {
                    //Return statements have been validated. It probably returned in some other block earlier.
                    //LLVM requires return statement so just return default
                    LLVMSharp.LLVM.BuildRet(pContext.Builder, SmallTypeCache.GetLLVMDefault(Type, pContext));
                }

                //End method
                pContext.RemoveDeferredStatementExecution();
                pContext.Locals.RemoveScope();
                pContext.FinishMethod(f);
                return f;
            }

            return default;
        }
    }
}