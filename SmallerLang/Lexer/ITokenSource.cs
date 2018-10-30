using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmallerLang.Lexer
{
    interface ITokenSource
    {
        string Source { get; }
        string SourcePath { get; }
        int Index { get; }
        int Line { get; }
        int Column { get; }

        bool GetNextToken(out Token pToken);
    }
}
