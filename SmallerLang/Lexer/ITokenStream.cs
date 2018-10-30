using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmallerLang.Lexer
{
    public interface ITokenStream
    {
        string Source { get; }
        string SourcePath { get; }
        int SourceIndex { get; }
        int SourceLine { get; }
        int SourceColumn { get; }

        int Index { get; }
        ref Token Current { get; }
        bool EOF { get; }

        bool Peek(int pCount, out Token pToken);
        bool MoveNext();
        void Seek(int pIndex);
        void Reset();
    }
}
