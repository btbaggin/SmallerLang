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
            get
            {
                var t = SmallTypeCache.FromString(GetFullTypeName(this));
                if(t.IsGenericType)
                {
                    return t.MakeConcreteType(Utils.SyntaxHelper.SelectNodeTypes(GenericArguments));
                }
                return t;
            } 
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

        //TODO move me
        public static string GetFullTypeName(TypeSyntax pNode)
        {
            if (pNode.GenericArguments.Count == 0) return pNode.Value;

            var structName = new StringBuilder(pNode.Value);
            structName.Append("`");
            structName.Append(pNode.GenericArguments.Count);
            return structName.ToString();
        }

        public override string ToString()
        {
            return TypeSyntax.GetFullTypeName(this);
        }
    }
}
