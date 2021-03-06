﻿using System;
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
        ScopePublic,
        ScopePrivate,
        Const,

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
        LeftShift,
        RightShift,
        BitwiseAnd,
        BitwiseOr,
        Mod,

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
        TypeChar,
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
        QuestionMark,

        //Special tokens
        Annotation,
        Identifier,
        Newline,
        Unknown,
        EndOfFile
    }
}
