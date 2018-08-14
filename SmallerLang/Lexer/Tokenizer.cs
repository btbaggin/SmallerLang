using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmallerLang.Lexer
{
    public class Tokenizer
    {
        readonly ReadOnlyMemory<char> _string;

        public char Current
        {
            get
            {
                if (EOF) return (char)0;
                return _string.Span[Index];
            }
        }

        public int Line { get; private set; }

        public int Column { get; private set; }

        public int Index { get; private set; }

        public int TokenStart { get; private set; }

        public bool EOF
        {
            get { return Index >= _string.Length; }
        }

        public Tokenizer(string pstrString)
        {
            Line = 1;
            Column = 1;
            Index = 0;
            _string = pstrString.AsMemory();
        }

        public void Eat()
        {
            if (Current == '\n')
            {
                Line++;
                Column = 1;
            }
            else Column++;

            Index++;
        }

        public char Peek(int plngLookahead)
        {
            if (Index + plngLookahead >= _string.Length) return (char)0;
            return _string.Span[Index + plngLookahead];
        }

        public void StartToken()
        {
            TokenStart = Index;
        }

        internal ReadOnlySpan<char> GetSpan(int pLength)
        {
            return _string.Span.Slice(TokenStart, pLength);
        }

        internal ReadOnlyMemory<char> GetMemory(int pLength)
        {
            return _string.Slice(TokenStart, pLength);
        }

        internal ReadOnlyMemory<char> GetMemory(int pStart, int pLength)
        {
            return _string.Slice(pStart, pLength);
        }
    }
}
