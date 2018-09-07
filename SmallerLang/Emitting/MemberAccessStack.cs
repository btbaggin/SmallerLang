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
    class MemberAccessStack<T>
    {
        public int Count { get; private set; }

        T[] _items;
        public MemberAccessStack()
        {
            _items = new T[8];
        }

        public void Push(T pItem)
        {
            if(Count >= _items.Length)
            {
                Array.Resize(ref _items, _items.Length + 4);
            }
            _items[Count] = pItem;
            Count++;
        }

        public T Pop()
        {
            return _items[--Count];
        }

        public T Peek()
        {
            return _items[Count - 1];
        }

        public T PeekAt(int pIndex)
        {
            if (pIndex >= Count) throw new ArgumentException("pIndex must be greater than 0 and less than Count");
            return _items[pIndex];
        }
    }
}
