using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Syntax;
using SmallerLang.Lexer;

namespace SmallerLang.Utils
{
    static class EnumExtensions
    {
        public static BinaryExpressionOperator ToBinaryExpression(this TokenType pToken)
        {
            switch(pToken)
            {
                case TokenType.EqualsEquals:
                    return BinaryExpressionOperator.Equals;

                case TokenType.NotEquals:
                    return BinaryExpressionOperator.NotEquals;

                case TokenType.LessThan:
                    return BinaryExpressionOperator.LessThan;

                case TokenType.LessThanOrEqual:
                    return BinaryExpressionOperator.LessThanOrEqual;

                case TokenType.GreaterThan:
                    return BinaryExpressionOperator.GreaterThan;

                case TokenType.GreaterThanOrEqual:
                    return BinaryExpressionOperator.GreaterThanOrEqual;

                case TokenType.Plus:
                    return BinaryExpressionOperator.Addition;

                case TokenType.Minus:
                    return BinaryExpressionOperator.Subtraction;

                case TokenType.Star:
                    return BinaryExpressionOperator.Multiplication;

                case TokenType.Division:
                    return BinaryExpressionOperator.Division;

                case TokenType.Percent:
                    return BinaryExpressionOperator.Mod;

                case TokenType.And:
                    return BinaryExpressionOperator.And;

                case TokenType.Or:
                    return BinaryExpressionOperator.Or;

                case TokenType.LeftShift:
                    return BinaryExpressionOperator.LeftBitShift;

                case TokenType.RightShift:
                    return BinaryExpressionOperator.RightBitShift;

                case TokenType.BitwiseAnd:
                    return BinaryExpressionOperator.BitwiseAnd;

                case TokenType.BitwiseOr:
                    return BinaryExpressionOperator.BitwiseOr;

                case TokenType.Mod:
                    return BinaryExpressionOperator.Mod;

                default:
                    throw new ArgumentException("Not a supported binary expression token " + pToken.ToString());
            }
        }

        public static AssignmentOperator ToAssignmentOperator(this TokenType pToken)
        {
            switch(pToken)
            {
                case TokenType.Equals:
                    return AssignmentOperator.Equals;

                case TokenType.MinusEquals:
                    return AssignmentOperator.SubtractionEquals;

                case TokenType.PlusEquals:
                    return AssignmentOperator.AdditionEquals;

                case TokenType.StarEquals:
                    return AssignmentOperator.MultiplyEquals;

                case TokenType.DivisionEquals:
                    return AssignmentOperator.DivisionEquals;

                case TokenType.ConcatenateEquals:
                    return AssignmentOperator.ConcatEquals;

                default:
                    throw new NotSupportedException("Not a supported assignment operator token " + pToken.ToString());
            }
        }

        public static UnaryExpressionOperator ToUnaryExpression(this TokenType pToken, bool pPre)
        {
            switch(pToken)
            {
                case TokenType.Bang:
                    return UnaryExpressionOperator.Not;

                case TokenType.Minus:
                    return UnaryExpressionOperator.Negative;

                case TokenType.LengthOf:
                    return UnaryExpressionOperator.Length;

                case TokenType.PlusPlus:
                    return pPre ? UnaryExpressionOperator.PreIncrement : UnaryExpressionOperator.PostIncrement;

                case TokenType.MinusMinus:
                    return pPre ? UnaryExpressionOperator.PreDecrement : UnaryExpressionOperator.PostDecrement;

                default:
                    throw new ArgumentException("Not a supported unary expression token " + pToken.ToString());
            }
        }
    }
}
