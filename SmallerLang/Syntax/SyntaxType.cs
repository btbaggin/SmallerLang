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
        Break,
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
        Namespace,
        NumericLiteral,
        Return,
        Select,
        Self,
        StringLiteral,
        StructInitializer,
        TernaryExpression,
        TypeDefinition,
        TypedIdentifier,
        Type,
        UnaryExpression,
        While,
        Workspace,

        None
    }
}
