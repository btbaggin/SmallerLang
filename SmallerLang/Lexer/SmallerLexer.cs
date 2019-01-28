using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using SmallerLang.Utils;

namespace SmallerLang.Lexer
{
    public class SmallerLexer : ITokenSource
    {
        Tokenizer _tokenizer;
        int _length;
        bool _atEnd;
        readonly Trie _keywords;

        #region Properties
        public string Source { get; private set; }

        public string SourcePath { get; private set; }

        public int Index
        {
            get { return _tokenizer.Index; }
        }

        public int Line
        {
            get { return _tokenizer.Line; }
        }

        public int Column
        {
            get { return _tokenizer.Column; }
        }
        #endregion

        public SmallerLexer()
        {
            _keywords = new Trie();

            //Keywords
            _keywords.Insert("let", TokenType.Let);
            _keywords.Insert("if", TokenType.If);
            _keywords.Insert("while", TokenType.While);
            _keywords.Insert("for", TokenType.For);
            _keywords.Insert("else", TokenType.Else);
            _keywords.Insert("return", TokenType.Return);
            _keywords.Insert("true", TokenType.True);
            _keywords.Insert("false", TokenType.False);
            _keywords.Insert("extern", TokenType.Extern);
            _keywords.Insert("select", TokenType.Select);
            _keywords.Insert("case", TokenType.Case);
            _keywords.Insert("it", TokenType.It);
            _keywords.Insert("struct", TokenType.Struct);
            _keywords.Insert("trait", TokenType.Trait);
            _keywords.Insert("impl", TokenType.Implement);
            _keywords.Insert("on", TokenType.On);
            _keywords.Insert("new", TokenType.New);
            _keywords.Insert("defer", TokenType.Defer);
            _keywords.Insert("cast", TokenType.Cast);
            _keywords.Insert("enum", TokenType.Enum);
            _keywords.Insert("self", TokenType.Self);
            _keywords.Insert("lengthof", TokenType.LengthOf);
            _keywords.Insert("import", TokenType.Import);
            _keywords.Insert("break", TokenType.Break);
            _keywords.Insert("public", TokenType.ScopePublic);
            _keywords.Insert("private", TokenType.ScopePrivate);
            _keywords.Insert("const", TokenType.Const);

            //Types
            _keywords.Insert("float", TokenType.TypeFloat);
            _keywords.Insert("double", TokenType.TypeDouble);
            _keywords.Insert("short", TokenType.TypeShort);
            _keywords.Insert("int", TokenType.TypeInt);
            _keywords.Insert("long", TokenType.TypeLong);
            _keywords.Insert("string", TokenType.TypeString);
            _keywords.Insert("char", TokenType.TypeChar);
            _keywords.Insert("bool", TokenType.TypeBool);

            //Various trivia
            _keywords.Insert("\n", TokenType.Newline);
            _keywords.Insert("(", TokenType.LeftParen);
            _keywords.Insert(")", TokenType.RightParen);
            _keywords.Insert("[", TokenType.LeftBracket);
            _keywords.Insert("]", TokenType.RightBracket);
            _keywords.Insert("{", TokenType.LeftBrace);
            _keywords.Insert("}", TokenType.RightBrace);
            _keywords.Insert(":", TokenType.Colon);
            _keywords.Insert("::", TokenType.ColonColon);
            _keywords.Insert(",", TokenType.Comma);
            _keywords.Insert(".", TokenType.Period);

            //Operators
            _keywords.Insert("*", TokenType.Star);
            _keywords.Insert(@"/", TokenType.Division);
            _keywords.Insert("+", TokenType.Plus);
            _keywords.Insert("-", TokenType.Minus);
            _keywords.Insert("*=", TokenType.StarEquals);
            _keywords.Insert(@"/=", TokenType.DivisionEquals);
            _keywords.Insert("+=", TokenType.PlusEquals);
            _keywords.Insert("-=", TokenType.MinusEquals);
            _keywords.Insert("->", TokenType.DashGreater);
            _keywords.Insert("!", TokenType.Bang);
            _keywords.Insert("=", TokenType.Equals);
            _keywords.Insert("==", TokenType.EqualsEquals);
            _keywords.Insert("!=", TokenType.NotEquals);
            _keywords.Insert("<", TokenType.LessThan);
            _keywords.Insert("<=", TokenType.LessThanOrEqual);
            _keywords.Insert(">", TokenType.GreaterThan);
            _keywords.Insert(">=", TokenType.GreaterThan);
            _keywords.Insert("++", TokenType.PlusPlus);
            _keywords.Insert("--", TokenType.MinusMinus);
            _keywords.Insert("**", TokenType.StarStar);
            _keywords.Insert("&&", TokenType.And);
            _keywords.Insert("||", TokenType.Or);
            _keywords.Insert("_", TokenType.Underscore);
            _keywords.Insert("#", TokenType.Hash);
            _keywords.Insert("<<", TokenType.LeftShift);
            _keywords.Insert(">>", TokenType.RightShift);
            _keywords.Insert("%", TokenType.Mod);
            _keywords.Insert("&", TokenType.BitwiseAnd);
            _keywords.Insert("|", TokenType.BitwiseOr);
        }

        public ITokenStream StartTokenStream(string pstrSource, string pPath)
        {
            Source = pstrSource;
            SourcePath = pPath;
            _atEnd = false;
            _tokenizer = new Tokenizer(pstrSource);
            return new BufferedTokenStream(this);
        }

        public bool GetNextToken(out Token pToken)
        {
            Token current;
            do
            {
                current = NextSymbol();
            } while (current.Type == TokenType.Unknown);

            if (current.Type != TokenType.EndOfFile)
            {
                pToken = current;
                return true;
            }

            pToken = new Token(TokenType.EndOfFile, 3);
            if (!_atEnd)
            {
                _atEnd = true;
                return true;
            }

            return false;
        }

        private Token NextSymbol()
        {
            SkipWhitespace();
            SkipComments();
            if (_tokenizer.EOF) return new Token(TokenType.EndOfFile, 3);

            _length = 0;
            _tokenizer.StartToken();

            //Identifiers
            if (char.IsLetter(_tokenizer.Current) || _tokenizer.Current == '_')
            {
                return CreateIdentifier();
            }

            //Numbers
            if (char.IsDigit(_tokenizer.Current))
            {
                return CreateNumber();
            }

            //Strings
            if (_tokenizer.Current == '"')
            {
                return CreateString();
            }

            //Annotation
            if (_tokenizer.Current == '@')
            {
                return CreateAnnotation();
            }

            //Symbols and keywords
            TrieNode t = _keywords.Root;
            TrieNode result = t;
            while ((t = t.FindChild(_tokenizer.Current)) != null)
            {
                result = t;
                Eat();
            }
            if(!result.Leaf)
            {
                var span = new TextSpan(_tokenizer.Index, _tokenizer.Index + 1, _tokenizer.Line, _tokenizer.Column, Source, SourcePath);
                CompilerErrors.UnknownCharacter(_tokenizer.Current, span);
                Eat();
                return CreateToken(TokenType.Unknown);
            }

            return CreateToken(result.Type);
        }

        private void SkipWhitespace()
        {
            while(!_tokenizer.EOF)
            {
                switch(_tokenizer.Current)
                {
                    case ' ':
                    case '\t':
                    case '\r':
                        _tokenizer.Eat();
                        break;

                    default:
                        return;
                }
            }
        }

        private void SkipComments()
        {
            if(_tokenizer.Current == '/')
            {
                switch(_tokenizer.Peek(1))
                {
                    case '/':
                        //Single line comments
                        _tokenizer.Eat();
                        _tokenizer.Eat();
                        while (_tokenizer.Current != '\n') { _tokenizer.Eat(); }
                        break;

                    case '*':
                        //Multi line comments
                        //Consume /*
                        _tokenizer.Eat();
                        _tokenizer.Eat();
                        while (!_tokenizer.EOF)
                        {
                            if (_tokenizer.Current == '*' && _tokenizer.Peek(1) == '/') break;
                            _tokenizer.Eat();
                        }
                        //Consume */
                        _tokenizer.Eat();
                        _tokenizer.Eat();
                        if (_tokenizer.Current == '\r') _tokenizer.Eat();
                        break;
                }
            }
        }

        private Token CreateToken(TokenType penmType)
        {
            return new Token(penmType, _length);
        }

        private Token CreateToken(TokenType penmType, ReadOnlyMemory<char> pstrValue)
        {
            return new Token(penmType, _length, pstrValue);
        }

        private void Eat()
        {
            _tokenizer.Eat();
            _length++;
        }

        private Token CreateIdentifier()
        {
            do
            {
                Eat();
            }
            while (char.IsLetterOrDigit(_tokenizer.Current) || _tokenizer.Current == '_');

            var i = _tokenizer.GetSpan(_length);
            TrieNode t = _keywords.Prefix(i);
            //Need to check length to prevent things like "item" from saying "it" or "_variable" as "_"
            if (t.Leaf && t.Depth == i.Length)
            {
                return CreateToken(t.Type);
            }

            return CreateToken(TokenType.Identifier);
        }

        private Token CreateNumber()
        {
            TokenType type = TokenType.Integer;
            while(char.IsDigit(_tokenizer.Current))
            {
                Eat();
            }

            if (_tokenizer.Current == '.' && char.IsDigit(_tokenizer.Peek(1)))
            {
                Eat();
                type = TokenType.Float;
            }

            while(char.IsDigit(_tokenizer.Current))
            {
                Eat();
            }

            int mod = 0;
            switch (_tokenizer.Current)
            {
                case 'i':
                    Eat();
                    type = TokenType.Integer;
                    mod = 1;
                    break;

                case 's':
                    Eat();
                    type = TokenType.Short;
                    mod = 1;
                    break;

                case 'l':
                    Eat();
                    type = TokenType.Long;
                    mod = 1;
                    break;

                case 'f':
                    Eat();
                    type = TokenType.Float;
                    mod = 1;
                    break;

                case 'd':
                    Eat();
                    type = TokenType.Double;
                    mod = 1;
                    break;
            }

            return CreateToken(type, _tokenizer.GetMemory(_length - mod));
        }

        private Token CreateString()
        {
            Eat(); // Consume opening "
            bool inEscape = false;
            while(!_tokenizer.EOF && (_tokenizer.Current != '"' || inEscape))
            {
                if(inEscape)
                {
                    switch (_tokenizer.Current)
                    {
                        case 'n':
                        case 'r':
                        case '\\':
                        case '"':
                            inEscape = false;
                            break;
                    }
                }
                else
                {
                    inEscape = _tokenizer.Current == '\\';
                }
                Eat();
            }
            Eat(); //Consume closing "

            return CreateToken(TokenType.String, _tokenizer.GetMemory(_tokenizer.TokenStart + 1, _length - 2));
        }

        private Token CreateAnnotation()
        {
            Eat(); //Consume @
            while(!_tokenizer.EOF && _tokenizer.Current != '\n' && _tokenizer.Current != '\r')
            {
                Eat();
            }
            return CreateToken(TokenType.Annotation, _tokenizer.GetMemory(_tokenizer.TokenStart + 1, _length - 1));
        }
    }
}
