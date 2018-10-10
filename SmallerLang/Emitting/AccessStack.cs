using System;
using System.Collections.Generic;
using LLVMSharp;

namespace SmallerLang.Emitting
{
    public struct MemberAccess
    {
        public LLVMValueRef Value { get; }
        public SmallType Type { get; }

        public MemberAccess(LLVMValueRef pValue, SmallType pType)
        {
            Value = pValue;
            Type = pType;
        }
    }

    //Special stack that allows arbitrary index access
    public class AccessStack<T>
    {
        public int Count { get; private set; }

        T[] _items;
        public AccessStack()
        {
            _items = new T[8];
        }

        public AccessStack(int pCapacity)
        {
            _items = new T[pCapacity];
        }

        public int Push(T pValue)
        {
            if(Count >= _items.Length)
            {
                Array.Resize(ref _items, _items.Length + 4);
            }
            _items[Count] = pValue;
            return Count++;
        }

        public void PopFrom(int pIndex)
        {
            if(pIndex < Count) Count--;
        }

        public void Pop()
        {
            Count--;
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

        public AccessStack<T> Copy()
        {
            var copy = new AccessStack<T>
            {
                _items = _items,
                Count = Count
            };
            return copy;
        }

        public void Clear()
        {
            Count = 0;
        }

        public static LLVMValueRef BuildGetElementPtr(EmittingContext pContext, LLVMValueRef? pValue)
        {
            List<LLVMValueRef> indexes = new List<LLVMValueRef>(pContext.AccessStack.Count)
            {
                pContext.GetInt(0)
            };

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
