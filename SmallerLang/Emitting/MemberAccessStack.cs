using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;

namespace SmallerLang.Emitting
{
    struct MemberAccessItem
    {
        public LLVMValueRef Value { get; }
        public SmallType Type { get; }

        public MemberAccessItem(LLVMValueRef pValue, SmallType pType)
        {
            Value = pValue;
            Type = pType;
        }
    }

    //Special stack that allows arbitrary index access
    class MemberAccessStack
    {
        public int Count { get; private set; }

        MemberAccessItem[] _items;
        public MemberAccessStack()
        {
            _items = new MemberAccessItem[8];
        }

        public void Push(LLVMValueRef pValue, SmallType pType)
        {
            if(Count >= _items.Length)
            {
                Array.Resize(ref _items, _items.Length + 4);
            }
            _items[Count] = new MemberAccessItem(pValue, pType);
            Count++;
        }

        public MemberAccessItem Pop()
        {
            return _items[--Count];
        }

        public MemberAccessItem Peek()
        {
            return _items[Count - 1];
        }

        public MemberAccessItem PeekAt(int pIndex)
        {
            if (pIndex >= Count) throw new ArgumentException("pIndex must be greater than 0 and less than Count");
            return _items[pIndex];
        }

        public MemberAccessStack Copy()
        {
            var m = new MemberAccessStack();
            m._items = _items;
            m.Count = Count;
            return m;
        }

        public void Clear()
        {
            Count = 0;
        }

        public static LLVMValueRef BuildGetElementPtr(EmittingContext pContext, LLVMValueRef? pValue)
        {
            List<LLVMValueRef> indexes = new List<LLVMValueRef>(pContext.AccessStack.Count);
            indexes.Add(pContext.GetInt(0));

            for (int j = pContext.AccessStack.Count - 1; j > 0; j--)
            {
                indexes.Add(pContext.AccessStack.PeekAt(j).Value);
            }
            if(pValue.HasValue) indexes.Add(pValue.Value);
            var i = pContext.AccessStack.PeekAt(0).Value;
            return LLVM.BuildInBoundsGEP(pContext.Builder, i, indexes.ToArray(), "memberaccess");
        }
    }
}
