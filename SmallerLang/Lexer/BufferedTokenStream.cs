using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmallerLang.Lexer
{
    class BufferedTokenStream : ITokenStream
    {
        public string Source { get => mSource.Source; }

        public ref Token Current { get { return ref _tokens[Index]; } }

        public bool EOF { get => Index >= _tokenCount; }

        public int Index { get; private set; }

        public int SourceIndex { get; private set; }

        public int SourceLine { get; private set; }

        public int SourceColumn { get; private set; }

        readonly ITokenSource mSource;
        Token[] _tokens;
        int _tokenCount;
        Stack<int> mstkMarkers;

        public BufferedTokenStream(ITokenSource pSource)
        {
            mSource = pSource;
            mstkMarkers = new Stack<int>();
            _tokens = new Token[128];
            Index = 0;
            Fetch(1);
        }

        public bool MoveNext()
        {
            SourceLine = mSource.Line;
            SourceColumn = mSource.Column;
            Index++;
            Sync(Index);
            SourceIndex = mSource.Index - Current.Length;
            return _tokenCount > Index;
        }

        public bool Peek(int pCount, out Token pToken)
        {
            Sync(Index + pCount);
            if (Index + pCount >= _tokenCount)
            {
                pToken = default;
                return false;
            }

            pToken = _tokens[Index + pCount];
            return true;
        }

        public void Reset()
        {
            mstkMarkers = new Stack<int>();
            Index = 0;
        }

        private void Sync(int plngNum)
        {
            int n = plngNum - _tokenCount + 1;
            if (n > 0) Fetch(n);
        }

        private void Fetch(int plngNum)
        {
            for (int i = 0; i < plngNum; i++)
            {
                if (mSource.GetNextToken(out Token t))
                {
                    if(_tokens.Length <= _tokenCount)
                    {
                        Array.Resize(ref _tokens, _tokens.Length * 2);
                    }
                    _tokens[_tokenCount] = t;
                    _tokenCount++;
                }
            }
        }

        public void CommitTransaction()
        {
            if (mstkMarkers.Count > 0) mstkMarkers.Pop();
        }

        public void Seek(int pIndex)
        {
            Sync(pIndex);
            Index = pIndex;
        }
    }
}
