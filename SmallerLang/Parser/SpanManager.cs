using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Lexer;

namespace SmallerLang.Parser
{
    internal class SpanManager
    {
        readonly Stack<SpanTracker> _positions;
        readonly ITokenStream _stream;

        public TextSpan Current
        {
            get { return _positions.Peek(); }
        }

        public SpanManager(ITokenStream pStream)
        {
            _positions = new Stack<SpanTracker>();
            _stream = pStream;
        }

        public SpanTracker Create()
        {
            int start = _stream.SourceIndex;
            int line = _stream.SourceLine;
            int column = _stream.SourceColumn;
            SpanTracker s = new SpanTracker(start, line, column, this);
            _positions.Push(s);
            return s;
        }

        internal int GetEndIndex()
        {
            return _stream.SourceIndex - _stream.Current.Length;
        }

        internal string MapCurrentToSource()
        {
            TextSpan t = _positions.Peek();
            return _stream.Source.Substring(t.Start, t.Length);
        }

        internal void Pop()
        {
            if (_positions.Count > 0) _positions.Pop();
        }
    }

    internal struct SpanTracker : IDisposable
    {
        public int Start { get; }

        public int Line { get; }

        public int Column { get; }

        readonly SpanManager _manager;
        public SpanTracker(int pStart, int pLine, int pColumn, SpanManager pManager)
        {
            Start = pStart;
            Line = pLine;
            Column = pColumn;
            _manager = pManager;
        }

        private TextSpan ToTextSpan()
        {
            return new TextSpan(Start, _manager.GetEndIndex(), Line, Column);
        }

        public static implicit operator TextSpan(SpanTracker pT)
        {
            return pT.ToTextSpan();
        }

        public void Dispose()
        {
            _manager.Pop();
        }
    }

}
