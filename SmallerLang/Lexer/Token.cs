using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmallerLang.Lexer
{
    public struct Token
    {
        public TokenType Type { get; private set; }

        public int Length { get; private set; }

        public ReadOnlyMemory<char> Value { get; private set; }

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
