using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmallerLang.Lexer
{
    public enum TokenType
    {
        //Keywords
        Let,
        If,
        Else,
        While,
        For,
        Return,
        Extern,
        Struct,
        New,
        Select,
        Case,
        It,
        Defer,
        Cast,
        Enum,
        Self,
        Trait,
        Implement,
        On,
        Break,
        Import,

        //Struct queries
        LengthOf,

        //Operators
        And,
        Or,
        Bang,
        StarStar,
        EqualsEquals,
        Equals,
        NotEquals,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        Plus,
        Minus,
        Star,
        Division,
        Concatenate,
        Percent,
        PlusPlus,
        MinusMinus,
        PlusEquals,
        MinusEquals,
        StarEquals,
        DivisionEquals,
        ConcatenateEquals,

        //Types
        Integer,
        Double,
        Float,
        Short,
        Long,
        String,
        True,
        False,

        //Type keywords
        TypeFloat,
        TypeDouble,
        TypeShort,
        TypeInt,
        TypeLong,
        TypeString,
        TypeBool,
        Hash,

        //Other trivia
        Comma,
        Period,
        LeftBrace,
        RightBrace,
        LeftBracket,
        RightBracket,
        LeftParen,
        RightParen,
        Colon,
        ColonColon,
        DashGreater,
        Underscore,

        //Special tokens
        Annotation,
        Identifier,
        Newline,
        Unknown,
        EndOfFile
    }
}
