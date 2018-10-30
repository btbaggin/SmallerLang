using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmallerLang.Lexer
{
    class BufferedTokenStream : ITokenStream
    {
        public string Source { get => _source.Source; }

        public string SourcePath { get => _source.SourcePath; }

        public ref Token Current { get { return ref _tokens[Index]; } }

        public bool EOF { get => Index >= _tokenCount; }

        public int Index { get; private set; }

        public int SourceIndex { get; private set; }

        public int SourceLine { get; private set; }

        public int SourceColumn { get; private set; }

        readonly ITokenSource _source;
        Token[] _tokens;
        int _tokenCount;

        public BufferedTokenStream(ITokenSource pSource)
        {
            _source = pSource;
            _tokens = new Token[128];
            Index = 0;
            Fetch(1);
        }

        public bool MoveNext()
        {
            SourceLine = _source.Line;
            SourceColumn = _source.Column;
            Index++;
            Sync(Index);
            SourceIndex = _source.Index - Current.Length;
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
                if (_source.GetNextToken(out Token t))
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

        public void Seek(int pIndex)
        {
            Sync(pIndex);
            Index = pIndex;
        }
    }
}
