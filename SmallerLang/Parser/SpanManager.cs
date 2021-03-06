﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Lexer;

namespace SmallerLang.Parser
{
    internal class SpanManager
    {
        readonly Stack<(int Count, SpanTracker Span)> _positions;
        ITokenStream _stream;

        public TextSpan Current
        {
            get { return _positions.Peek().Span; }
        }

        public SpanManager(ITokenStream pStream)
        {
            _positions = new Stack<(int, SpanTracker)>();
            _stream = pStream;

            //Push a dummy tracker into the stack in case we access Current before creating trackers
            _positions.Push((0, new SpanTracker(-1, 0, 0, null, null, this)));
        }

        public void SetStream(ITokenStream pStream)
        {
            _stream = pStream;
        }

        public SpanTracker Create()
        {
            int start = _stream.SourceIndex;
            int line = _stream.SourceLine;
            int column = _stream.SourceColumn;

            SpanTracker s;
            //If we are starting at the same point, just increment the reference counter instead of allocating a new tracker
            if (start == Current.Start)
            {
                var entry = _positions.Pop();
                entry.Count++;
                _positions.Push(entry);
                s = entry.Span;
            }
            else
            {
                s = new SpanTracker(start, line, column, _stream.Source, _stream.SourcePath, this);
                _positions.Push((1, s));
            }
            
            return s;
        }

        internal int GetEndIndex()
        {
            return _stream.SourceIndex;
        }

        internal void Pop()
        {
            if (_positions.Count > 0)
            {
                var entry = _positions.Pop();
                if (entry.Count > 1)
                {
                    //If there is still more than one reference to this object, just decrement the counter
                    entry.Count--;
                    _positions.Push(entry);
                }
            }
        }
    }

    internal readonly struct SpanTracker : IDisposable
    {
        public int Start { get; }

        public int Line { get; }

        public int Column { get; }

        readonly string _file;
        readonly string _path;
        readonly SpanManager _manager;
        public SpanTracker(int pStart, int pLine, int pColumn, string pFile, string pPath, SpanManager pManager)
        {
            Start = pStart;
            Line = pLine;
            Column = pColumn;
            _manager = pManager;
            _file = pFile;
            _path = pPath;
        }

        private TextSpan ToTextSpan()
        {
            return new TextSpan(Start, _manager.GetEndIndex(), Line, Column, _file, _path);
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
