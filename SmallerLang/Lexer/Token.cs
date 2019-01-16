using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmallerLang.Lexer
{
    public readonly struct Token
    {
        public TokenType Type { get; }

        public int Length { get; }

        public ReadOnlyMemory<char> Value { get; }

        public Token(TokenType pType, int pLength)
        {
            Type = pType;
            Length = pLength;
            Value = null;
        }

        public Token(TokenType pType, int pLength, ReadOnlyMemory<char> pValue)
        {
            Type = pType;
            Length = pLength;
            Value = pValue;
        }
    }
}
