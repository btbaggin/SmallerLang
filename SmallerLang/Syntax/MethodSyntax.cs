using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Emitting;
using LLVMSharp;

namespace SmallerLang.Syntax
{
    public class MethodSyntax : SyntaxNode, IEquatable<MethodSyntax>
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
                return SmallTypeCache.GetOrCreateTuple(Utils.SyntaxHelper.SelectNodeTypes(ReturnValues));
            }
        }

        public override SyntaxType SyntaxType => SyntaxType.Method;

        string _name;
        internal MethodSyntax(string pName, IList<TypeSyntax> pReturns, IList<TypedIdentifierSyntax> pParameters, BlockSyntax pBody, bool pExternal)
        {
            Name = pName;
            ReturnValues = pReturns;
            Parameters = pParameters;
            Body = pBody;
            External = pExternal;
        }
        
        public LLVMValueRef EmitHeader(EmittingContext pContext)
        {
            //Emit header
            return pContext.EmitMethodHeader(Name, this, out _name);
        }

        public override LLVMValueRef Emit(EmittingContext pContext)
        {
            pContext.EmitDebugLocation(this);

            if (!External)
            {
                System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(_name), "Method name cannot be blank");
                var func = pContext.StartMethod(_name, this);
                pContext.AddDeferredStatementExecution();
                pContext.Locals.AddScope();
                pContext.AddDebugScope(Span);

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

                    //We want to dispose variables after deferred statements because
                    //then variables referenced in deferred statements will still be valid
                    BlockSyntax.BuildCallToDispose(pContext);
                }


                if(!lastIsReturn)
                {
                    if (ReturnValues.Count == 0) LLVM.BuildRetVoid(pContext.Builder);
                    else
                    {
                        //Return statements have been validated. It probably returned in some other block earlier.
                        //LLVM requires return statement so just return default
                        LLVM.BuildRet(pContext.Builder, SmallTypeCache.GetLLVMDefault(Type, pContext));
                    }
                }

                //End method
                pContext.RemoveDeferredStatementExecution();
                pContext.RemoveDebugScope();
                pContext.Locals.RemoveScope();
                pContext.FinishMethod(func);
                return func;
            }

            return default;
        }

        public override string ToString()
        {
            var name = new StringBuilder();
            name.Append(Name);
            name.Append("(");

            var parms = Utils.SyntaxHelper.SelectNodeTypes(Parameters);
            foreach(var p in parms)
            {
                name.Append(p.Name);
                name.Append(",");
            }
            if (parms.Length > 0) name = name.Remove(name.Length - 1, 1);

            name.Append(")");

            if (Type != SmallTypeCache.Undefined)
            {
                name.Append(" -> ");
                name.Append(Type.ToString());
            }

            return name.ToString();
        }

        public bool Equals(MethodSyntax other)
        {
            if (Name != other.Name) return false;

            if (Parameters.Count != other.Parameters.Count) return false;

            for (int i = 0; i < Parameters.Count; i++)
            {
                if (Parameters[i].Type != other.Parameters[i].Type) return false;
            }
            return true;
        }
    }
}
