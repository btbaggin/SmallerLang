using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Emitting;
using LLVMSharp;

namespace SmallerLang.Syntax
{
    public abstract class SyntaxNode
    {
        public bool IsMemberAccess
        {
            get { return GetType() == typeof(MemberAccessSyntax) || GetType() == typeof(ArrayAccessSyntax); }
        }

        public bool Deferred { get; internal set; }

        public TextSpan Span { get; private set; }

        public Annotation Annotation { get; internal set; }

        public abstract SmallType Type { get; }

        public T SetSpan<T>(TextSpan pSpan) where T : SyntaxNode
        {
            Span = pSpan;
            return (T)this;
        }

        public virtual T FromNode<T>(T pNode) where T : SyntaxNode
        {
            Span = pNode.Span;
            Annotation = pNode.Annotation;
            Deferred = pNode.Deferred;
            return (T)this;
        }

        public abstract LLVMValueRef Emit(EmittingContext pContext);
    }
}
