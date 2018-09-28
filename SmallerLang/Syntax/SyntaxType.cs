using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmallerLang.Syntax
{
    public enum SyntaxType
    {
        ArrayAccess,
        ArrayLiteral,
        Assignment,
        BinaryExpression,
        Block,
        BooleanLiteral,
        Case,
        CastDefinition,
        Cast,
        Declaration,
        Discard,
        Else,
        Enum,
        For,
        Identifier,
        If,
        It,
        MemberAccess,
        MethodCall,
        Method,
        Module,
        NumericLiteral,
        Return,
        Select,
        Self,
        StringLiteral,
        StructInitializer,
        TypeDefinition,
        TypedIdentifier,
        Type,
        UnaryExpression,
        While,

        None
    }
}
