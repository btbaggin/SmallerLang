using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmallerLang
{
    public struct TextSpan
    {
        public int Start { get; private set; }
        public int Length { get; private set; }
        public int Line { get; private set; }
        public int Column { get; private set; }
        public int End
        {
            get { return Start + Length; }
        }

        public TextSpan(int pStart, int pEnd, int pLine, int pColumn)
        {
            Start = pStart;
            Length = pEnd - pStart;
            Line = pLine;
            Column = pColumn;
        }

        public TextSpan(int pStart, int pLength)
        {
            Start = pStart;
            Length = pLength;
            Line = 0;
            Column = 0;
        }

        public string ToSource(string pSource)
        {
            return pSource.Substring(Start, Length);
        }

        public string GetContainingLine(string pSource)
        {
            var lineStart = Start - Column - 1;
            var lineEnd = pSource.IndexOf('\n', End);
            if (lineEnd == -1) lineEnd = pSource.Length;
            return pSource.Substring(lineStart, lineEnd - lineStart);
        }
    }
}
