using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Emitting;

namespace SmallerLang.Syntax
{
    public class TypeSyntax : SyntaxNode
    {
        public override SmallType Type
        {
            get { return SmallTypeCache.FromString(GetFullTypeName(this)); } 
        }

        public override SyntaxType SyntaxType => SyntaxType.Type;

        public IList<TypeSyntax> GenericArguments { get; private set; }

        public string Value { get; private set; }

        public string Namespace { get; private set; }

        internal TypeSyntax(string pNamespace, string pValue, IList<TypeSyntax> pGenericArgs)
        {
            Namespace = pNamespace;
            if (Namespace == null) Value = pValue;
            else Value = Namespace + "." + pValue;
            GenericArguments = pGenericArgs;
        }

        public override LLVMSharp.LLVMValueRef Emit(EmittingContext pContext)
        {
            //No generation for type syntax
            throw new NotImplementedException();
        }

        public static string GetFullTypeName(TypeSyntax pNode)
        {
            if (pNode.GenericArguments.Count == 0) return pNode.Value;

            var structName = new StringBuilder(pNode.Value);
            structName.Append("<");
            for (int i = 0; i < pNode.GenericArguments.Count; i++)
            {
                structName.Append(pNode.GenericArguments[i].Value + ",");
            }
            structName = structName.Remove(structName.Length - 1, 1);
            structName.Append(">");
            return structName.ToString();
        }
    }
}
