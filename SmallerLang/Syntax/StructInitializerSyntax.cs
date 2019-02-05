using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using SmallerLang.Emitting;

namespace SmallerLang.Syntax
{
    public class StructInitializerSyntax : SyntaxNode
    {
        public List<IdentifierSyntax> Values { get; private set; }

        public TypeSyntax Struct { get; private set; }

        public List<SyntaxNode> Arguments { get; private set; }

        public override SmallType Type => Struct.Type;

        public override SyntaxType SyntaxType => SyntaxType.StructInitializer;

        internal StructInitializerSyntax(List<IdentifierSyntax> pValue, TypeSyntax pStruct, List<SyntaxNode> pArguments)
        {
            Values = pValue;
            Struct = pStruct;
            Arguments = pArguments;
        }

        public override LLVMValueRef Emit(EmittingContext pContext)
        {
            pContext.EmitDebugLocation(this); 

            var m = Type.GetConstructor();

            //Used in a return or some other expression.
            //Not being used in an assignment
            if(Values.Count == 0)
            {
                //We return a value here because we need to pass the value on to the rest of the expression
                var variable = pContext.AllocateVariable("return_temp", Type);
                BuildCallToConstructor(m, variable, pContext);
                return variable;
            }
            else
            {
                foreach (var v in Values)
                {
                    v.DoNotLoad = true;
                    var variable = v.Emit(pContext);
                    if (v.Type != Type)
                        {
                            //Implicitly cast any derived types
                            //This is the type itself so if you are doing Trait: t = new Concrete() where Concrete -> Trait
                            var t = SmallTypeCache.GetLLVMType(Type, pContext);
                            Utils.LlvmHelper.MakePointer(variable, ref t);
                            variable = LLVM.BuildBitCast(pContext.Builder, variable, t, "");
                    }

                    BuildCallToConstructor(m, variable, pContext);
                }

                //This isn't used in an expression so just calling the constructor on the value is enough
                return default;
            }
        }

        private void BuildCallToConstructor(in MethodDefinition pDef, LLVMValueRef pType, EmittingContext pContext)
        {
            LLVMValueRef[] arguments = new LLVMValueRef[Arguments.Count + 1];
            arguments[0] = pType;

            if (pDef.Name == TypeConstructors.StringFromCharsName)
            {
                TypeConstructors.StringFromChars(pType, Arguments[0], pContext);
            }
            else
            {
                for (int i = 0; i < Arguments.Count; i++)
                {
                    arguments[i + 1] = Arguments[i].Emit(pContext);

                    //Load the location of any pointer calculations
                    var op = arguments[i + 1].GetInstructionOpcode();
                    if (op == LLVMOpcode.LLVMGetElementPtr) arguments[i + 1] = LLVM.BuildLoad(pContext.Builder, arguments[i + 1], "arg_" + i.ToString());

                    //Implicitly cast any derived types
                    if (pDef.ArgumentTypes[i] != Arguments[i].Type)
                    {
                        var t = SmallTypeCache.GetLLVMType(pDef.ArgumentTypes[i], pContext);
                        Utils.LlvmHelper.MakePointer(arguments[i + 1], ref t);
                        arguments[i + 1] = LLVM.BuildBitCast(pContext.Builder, arguments[i + 1], t, "");
                    }
                }

                //Call constructor for all structs
                LLVM.BuildCall(pContext.Builder, pContext.GetMethod(pDef.MangledName), arguments, "");
            }           
        }
    }
}
