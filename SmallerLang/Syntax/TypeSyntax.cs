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
        private SmallType _type;
        public override SmallType Type
        {
            get { return _type; }
        }

        public override SyntaxType SyntaxType => SyntaxType.Type;

        public List<TypeSyntax> GenericArguments { get; private set; }

        public string Value { get; private set; }

        public NamespaceSyntax Namespace { get; private set; }

        internal TypeSyntax(NamespaceSyntax pNamespace, string pValue, List<TypeSyntax> pGenericArgs)
        {
            Namespace = pNamespace;
            Value = pValue;
            GenericArguments = pGenericArgs;

            SmallTypeCache.TryGetPrimitive(Value, out _type);//Get primitive types
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

        public void SetType(SmallType pType)
        {
            _type = pType;
        }

        public override T FromNode<T>(T pNode)
        {
            _type = pNode.Type;
            return base.FromNode(pNode);
        }
    }
}
