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
        public string Source { get; private set; }
        public string Path { get; private set; }

        public TextSpan(int pStart, int pEnd, int pLine, int pColumn, string pFile, string pPath)
        {
            Start = pStart;
            Length = pEnd - pStart;
            Line = pLine;
            Column = pColumn;
            Source = pFile;
            Path = pPath;
        }

        public string GetContainingLine()
        {
            var source = Source;
            var lineStart = Start - Column;
            if (Start != Column) lineStart--;

            var lineEnd = source.IndexOf('\n', End);
            if (lineEnd == -1) lineEnd = source.Length;
            return source.Substring(lineStart, lineEnd - lineStart);
        }
    }
}
