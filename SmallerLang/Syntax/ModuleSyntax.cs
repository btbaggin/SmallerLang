using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using SmallerLang.Emitting;

namespace SmallerLang.Syntax
{
    public class ModuleSyntax : SyntaxNode
    {
        public string Name { get; private set; }

        public string Namespace { get; private set; }

        public IList<MethodSyntax> Methods { get; private set; }

        public IList<TypeDefinitionSyntax> Structs { get; private set; }

        public IList<EnumSyntax> Enums { get; private set; }

        public override SmallType Type => SmallTypeCache.Undefined;

        public override SyntaxType SyntaxType => SyntaxType.Module;

        internal ModuleSyntax(string pNamespace, string pName, IList<MethodSyntax> pMethods, IList<TypeDefinitionSyntax> pDefinitions, IList<EnumSyntax> pEnums)
        {
            Namespace = pNamespace;
            Name = pName;
            Methods = pMethods;
            Structs = pDefinitions;
            Enums = pEnums;
        }

        public override LLVMValueRef Emit(EmittingContext pContext)
        {
            //Emit types. Need to do it in order of dependencies so all types resolve
            foreach(var i in Structs.OrderBy((pS) => pS.EmitOrder))
            {
                i.Emit(pContext);
            }

            //Emit type methods headers
            for (int i = 0; i < Structs.Count; i++)
            {
                Structs[i].EmitMethodHeaders(pContext);
            }

            //Emit method Headers
            LLVMValueRef _main = default;
            for(int i = 0; i < Methods.Count; i++)
            {
                var m = Methods[i].EmitHeader(pContext);
                if(Methods[i].Annotation.Value == Utils.KeyAnnotations.RunMethod)
                {
                    _main = m;
                }
            }

            //Emit type methods
            for (int i = 0; i < Structs.Count; i++)
            {
                Structs[i].EmitMethods(pContext);
            }

            //Emit method bodies
            for (int i = 0; i < Methods.Count; i++)
            {
                Methods[i].Emit(pContext);
            }

            pContext.FinishDebug();

            return _main;
        }
    }
}
