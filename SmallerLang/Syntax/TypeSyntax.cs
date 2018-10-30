using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Emitting;
using SmallerLang.Utils;

namespace SmallerLang.Syntax
{
    public class TypeSyntax : SyntaxNode
    {
        public override SmallType Type
        {
            get
            {
                var t = SmallTypeCache.FromString(SyntaxHelper.GetFullTypeName(this));
                if(t.IsGenericType)
                {
                    return t.MakeConcreteType(SyntaxHelper.SelectNodeTypes(GenericArguments));
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

        public override string ToString()
        {
            return SyntaxHelper.GetFullTypeName(this);
        }
    }
}
