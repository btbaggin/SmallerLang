using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using SmallerLang.Lexer;
using SmallerLang.Syntax;
using SmallerLang.Utils;

namespace SmallerLang.Parser
{
    //http://www.craftinginterpreters.com/parsing-expressions.html
    public class SmallerParser
    {
        private Token Current { get { return _stream.Current; } }

        readonly IErrorReporter _error;
        readonly ITokenStream _stream;
        ReadOnlyMemory<char> _source;
        readonly SpanManager _spans;
        private bool _allowIt;
        private bool _allowSelf;

        public SmallerParser(ITokenStream pStream, IErrorReporter pError)
        {
            _stream = pStream;
            _error = pError;
            _spans = new SpanManager(_stream);
        }

        public ModuleSyntax Parse()
        {
            _source = _stream.Source.AsMemory();
            return ParseModule();
        }

        private ModuleSyntax ParseModule()
        {
            //Module name
            Expect(TokenType.LeftBracket);
            Expect(TokenType.Identifier, out string name);
            Expect(TokenType.RightBracket);

            //Module content
            List<MethodSyntax> methods = new List<MethodSyntax>();
            List<TypeDefinitionSyntax> definitions = new List<TypeDefinitionSyntax>();
            List<EnumSyntax> enums = new List<EnumSyntax>();
            try
            {
                while (!Peek(TokenType.EndOfFile))
                {
                    IgnoreNewlines();
                    switch (Current.Type)
                    {
                        case TokenType.Enum:
                            enums.Add(ParseEnum());
                            break;

                        case TokenType.Extern:
                            methods.Add(ParseExtern());
                            break;

                        case TokenType.Identifier:
                            methods.Add(ParseMethod());
                            break;

                        case TokenType.Cast:
                            methods.Add(ParseCastDefinition());
                            break;

                        case TokenType.Struct:
                        case TokenType.Trait:
                        case TokenType.Implement:
                            definitions.Add(ParseTypeDefinition());
                            break;

                        case TokenType.EndOfFile:
                            break;

                        default:
                            _error.WriteError($"Unknown token {Current.Type}");
                            Ignore(Current.Type);
                            break;
                    }
                }
            }
            catch (ParseException)
            {
                //Some error occurred while parsing the method
                //Try to move to the next statement
                Synchronize();
                return null;
            }

            return SyntaxFactory.Module(name, methods, definitions, enums);
        }

        private EnumSyntax ParseEnum()
        {
            using (SpanTracker t = _spans.Create())
            {
                //Enum header
                Expect(TokenType.Enum);
                Expect(TokenType.Identifier, out string name);
                Ignore(TokenType.Newline);

                Expect(TokenType.LeftBrace);
                IgnoreNewlines();

                //Enum values
                List<IdentifierSyntax> names = new List<IdentifierSyntax>();
                List<int> values = new List<int>();
                var i = 0;
                while(!PeekAndExpect(TokenType.RightBrace))
                {
                    names.Add(ParseIdentifier());
                    if(PeekAndExpect(TokenType.Equals) && PeekAndExpect(TokenType.Integer, out string v))
                    {
                        i = int.Parse(v);
                    }
                    IgnoreNewlines();
                    values.Add(i);
                    i++;
                }

                return SyntaxFactory.Enum(name, names, values).SetSpan<EnumSyntax>(t);
            }
        }

        private TypeDefinitionSyntax ParseTypeDefinition()
        {
            using (SpanTracker t = _spans.Create())
            {
                //Struct name
                DefinitionTypes type = DefinitionTypes.Unknown;
                switch (Current.Type)
                {
                    case TokenType.Struct:
                        Expect(TokenType.Struct);
                        type = DefinitionTypes.Struct;
                        break;

                    case TokenType.Implement:
                        Expect(TokenType.Implement);
                        type = DefinitionTypes.Implement;
                        break;

                    case TokenType.Trait:
                        Expect(TokenType.Trait);
                        type = DefinitionTypes.Trait;
                        break;
                }

                Expect(TokenType.Identifier, out string name);

                //Struct generic type args
                List<string> genericTypeParms = new List<string>();
                if(PeekAndExpect(TokenType.LessThan))
                {
                    do
                    {
                        Expect(TokenType.Identifier, out string parmType);
                        genericTypeParms.Add(parmType);
                    } while (PeekAndExpect(TokenType.Comma));
                    Expect(TokenType.GreaterThan);
                }

                string implementOn = null;
                if (type == DefinitionTypes.Implement)
                {
                    Expect(TokenType.On);
                    Expect(TokenType.Identifier, out implementOn);
                }

                Ignore(TokenType.Newline);

                List<TypedIdentifierSyntax> fields = new List<TypedIdentifierSyntax>();
                List<MethodSyntax> methods = new List<MethodSyntax>();

                Expect(TokenType.LeftBrace);
                IgnoreNewlines();
                //Struct fields
                while(!PeekAndExpect(TokenType.RightBrace))
                {
                    if(_stream.Peek(1, out Token tok))
                    {
                        switch(tok.Type)
                        {
                            case TokenType.ColonColon:
                                _allowSelf = true;
                                methods.Add(ParseMethod(type != DefinitionTypes.Trait));
                                _allowSelf = false;
                                break;

                            default:
                                var ti = ParseTypedIdentifier();
                                ti.Annotation = ParseAnnotation();
                                fields.Add(ti);
                                break;
                        }
                    }
                    IgnoreNewlines();
                }

                return SyntaxFactory.TypeDefinition(name, implementOn, type, methods, fields, genericTypeParms).SetSpan<TypeDefinitionSyntax>(t);
            }
        }

        private MethodSyntax ParseExtern()
        {
            //Basically just the method header
            using (SpanTracker t = _spans.Create())
            {
                //Name
                Expect(TokenType.Extern);
                Expect(TokenType.Identifier, out string name);

                Expect(TokenType.ColonColon);
                Expect(TokenType.LeftParen);

                List<TypedIdentifierSyntax> parameters = new List<TypedIdentifierSyntax>();

                //Method parms
                if (!Peek(TokenType.RightParen))
                {
                    do
                    {
                        var i = ParseTypedIdentifier();
                        parameters.Add(i);
                    } while (PeekAndExpect(TokenType.Comma));
                }
                Expect(TokenType.RightParen);

                //Return type (if applicable)
                TypeSyntax r = null;
                if (PeekAndExpect(TokenType.DashGreater))
                {
                    r = ParseType();
                }
                Ignore(TokenType.Newline);

                var m = SyntaxFactory.ExternalMethod(name, r, parameters, null).SetSpan<MethodSyntax>(t);
                m.Annotation = ParseAnnotation();
                return m;
            }
        }

        private CastDefinitionSyntax ParseCastDefinition()
        {
            using (SpanTracker t = _spans.Create())
            {
                //Method name
                Expect(TokenType.Cast);
                Expect(TokenType.ColonColon);

                Expect(TokenType.LeftParen);
                var p = ParseTypedIdentifier();
                Expect(TokenType.RightParen);

                //Return type
                Expect(TokenType.DashGreater);
                var r = ParseType();
                Ignore(TokenType.Newline);

                //Method body
                var body = ParseBlock();
                return SyntaxFactory.CastDefinition(p, body, r).SetSpan<CastDefinitionSyntax>(t);
            }
        }

        private MethodSyntax ParseMethod(bool pExpectBody = true)
        {
            using (SpanTracker t = _spans.Create())
            {
                //Method name
                Expect(TokenType.Identifier, out string name);

                Expect(TokenType.ColonColon);
                Expect(TokenType.LeftParen);

                //Method parameters
                List<TypedIdentifierSyntax> parameters = new List<TypedIdentifierSyntax>();
                if(!Peek(TokenType.RightParen))
                {
                    do
                    {
                        var i = ParseTypedIdentifier();
                        parameters.Add(i);
                    } while (PeekAndExpect(TokenType.Comma));
                }
                Expect(TokenType.RightParen);

                //Return types
                List<TypeSyntax> returns = new List<TypeSyntax>();
                if (PeekAndExpect(TokenType.DashGreater))
                {
                    do
                    {
                        returns.Add(ParseType());
                    } while (PeekAndExpect(TokenType.Comma));
                }

                BlockSyntax body = null;
                if(pExpectBody)
                {
                    //Method body
                    Ignore(TokenType.Newline);
                    body = ParseBlock();
                }
                
                var m = SyntaxFactory.Method(name, returns, parameters, body).SetSpan<MethodSyntax>(t);

                //Annotations!
                m.Annotation = ParseAnnotation();
                return m;
            }
        }

        private TypeSyntax ParseType()
        {
            using (SpanTracker t = _spans.Create())
            {
                //Check for system types first, then an identifier as a user defined type
                string type;
                if (Peek(TokenType.TypeFloat)) Expect(TokenType.TypeFloat, out type);
                else if (Peek(TokenType.TypeDouble)) Expect(TokenType.TypeDouble, out type);
                else if (Peek(TokenType.TypeShort)) Expect(TokenType.TypeShort, out type);
                else if (Peek(TokenType.TypeInt)) Expect(TokenType.TypeInt, out type);
                else if (Peek(TokenType.TypeLong)) Expect(TokenType.TypeLong, out type);
                else if (Peek(TokenType.TypeString)) Expect(TokenType.TypeString, out type);
                else if (Peek(TokenType.TypeBool)) Expect(TokenType.TypeBool, out type);
                else Expect(TokenType.Identifier, out type);

                List<TypeSyntax> genericArgs = new List<TypeSyntax>();
                if(PeekAndExpect(TokenType.LessThan))
                {
                    do
                    {
                        genericArgs.Add(ParseType());
                    } while (PeekAndExpect(TokenType.Comma));
                    Expect(TokenType.GreaterThan);
                }

                //Check for array types
                if (PeekAndExpect(TokenType.LeftBracket))
                {
                    Expect(TokenType.RightBracket);
                    type += "[]";
                }
                return SyntaxFactory.Type(type, genericArgs).SetSpan<TypeSyntax>(t);
            }
        }

        private BlockSyntax ParseBlock()
        {
            using (SpanTracker t = _spans.Create())
            {
                Expect(TokenType.LeftBrace);
                IgnoreNewlines();

                List<SyntaxNode> statements = new List<SyntaxNode>();
                while (!PeekAndExpect(TokenType.RightBrace))
                {
                    var s = ParseStatement();
                    if (s != null) statements.Add(s);
                    IgnoreNewlines();
                }

                return SyntaxFactory.Block(statements).SetSpan<BlockSyntax>(t);
            }
        }

        private SyntaxNode ParseStatement()
        {
            try
            {
                SyntaxNode node;
                bool deferred = PeekAndExpect(TokenType.Defer);
                Ignore(TokenType.Newline);

                switch (Current.Type)
                {
                    case TokenType.If:
                        node = ParseIf();
                        break;

                    case TokenType.While:
                        node = ParseWhile();
                        break;

                    case TokenType.For:
                        node = ParseFor();
                        break;

                    case TokenType.Let:
                        node = ParseDeclaration();
                        break;

                    case TokenType.LeftBrace:
                        node = ParseBlock();
                        break;

                    case TokenType.Return:
                        node = ParseReturn();
                        break;

                    case TokenType.Select:
                        node = ParseSelect();
                        break;

                    case TokenType.Identifier:
                    case TokenType.It:
                    case TokenType.Self:
                        //Assignment with multiple identifiers is only allowed as a separate statement
                        node = ParseExpressionWithFullAssignment();
                        break;

                    default:
                        throw ReportError($"Encountered unknown token {Current.Type}", _spans.Current);
                }

                node.Deferred = deferred;
                return node;
            }
            catch (ParseException)
            {
                //Some error occurred while parsing the method
                //Try to move to the next statement
                Synchronize();
                return null;
            }
        }

        private ReturnSyntax ParseReturn()
        {
            using (SpanTracker t = _spans.Create())
            {
                Expect(TokenType.Return);
                List<SyntaxNode> values = new List<SyntaxNode>();
                do
                {
                    values.Add(ParseExpression());
                } while (PeekAndExpect(TokenType.Comma));
                return SyntaxFactory.Return(values).SetSpan<ReturnSyntax>(t);
            }
        }

        private DeclarationSyntax ParseDeclaration()
        {
            using (SpanTracker t = _spans.Create())
            {
                Expect(TokenType.Let);
                List<IdentifierSyntax> variables = new List<IdentifierSyntax>();
                do
                {
                    //We allow underscore to discard the value returned from that slot
                    var i = ParseIdentifier();
                    if (i == null) i = ParseDiscard();
                    if (i != null) variables.Add(i);
                } while (PeekAndExpect(TokenType.Comma));

                Expect(TokenType.Equals);

                //new only allowed in assignment and declaration statements
                SyntaxNode right = ParseExpression();
                var newStruct = ParseStructInitializer(variables);
                if (newStruct != null) right = newStruct;

                if(right == null)
                {
                    //Missing or invalid expression being assigned
                    ReportError("Expecting expression", t);
                    return null;
                }

                return SyntaxFactory.Declaration(variables, right).SetSpan<DeclarationSyntax>(t);
            }
        }
        
        private DiscardSyntax ParseDiscard()
        {
            using (SpanTracker t = _spans.Create())
            {
                if (PeekAndExpect(TokenType.Underscore)) return SyntaxFactory.Discard().SetSpan<DiscardSyntax>(t);
                return null;
            }
        }

        private StructInitializerSyntax ParseStructInitializer(IList<IdentifierSyntax> pIdentifier)
        {
            using (SpanTracker t = _spans.Create())
            {
                if (PeekAndExpect(TokenType.New))
                {
                    var type = ParseType();
                    Expect(TokenType.LeftParen);
                    List<SyntaxNode> arguments = new List<SyntaxNode>();
                    if(!Peek(TokenType.RightParen))
                    {
                        do
                        {
                            arguments.Add(ParseExpression());
                        } while (PeekAndExpect(TokenType.Comma));
                    }
                    Expect(TokenType.RightParen);

                    return SyntaxFactory.StructInitializer(pIdentifier, type, arguments).SetSpan<StructInitializerSyntax>(t);
                }
                return null;
            }
        }

        private IfSyntax ParseIf()
        {
            using (SpanTracker t = _spans.Create())
            {
                //If condition
                Expect(TokenType.If);
                Expect(TokenType.LeftParen);
                var condition = ParseExpression();
                Expect(TokenType.RightParen);

                //If body
                Ignore(TokenType.Newline);
                BlockSyntax body = null;
                if (Peek(TokenType.LeftBrace)) body = ParseBlock();
                else body = SyntaxFactory.SingleBlock(ParseStatement());
                Ignore(TokenType.Newline);

                //Else
                ElseSyntax e = ParseElse();

                return SyntaxFactory.If(condition, body, e).SetSpan<IfSyntax>(t);
            }
        }

        private ElseSyntax ParseElse()
        {
            using (SpanTracker t = _spans.Create())
            {
                //Else is not required so don't expect
                if (!PeekAndExpect(TokenType.Else)) return null;

                //Else will either have an if (else if) or a body (else), never both
                IfSyntax i = null;
                BlockSyntax body = null;
                if (Peek(TokenType.If)) i = ParseIf();
                else
                {
                    Ignore(TokenType.Newline);
                    if (Peek(TokenType.LeftBrace)) body = ParseBlock();
                    else body = SyntaxFactory.SingleBlock(ParseStatement());
                }

                return SyntaxFactory.Else(i, body).SetSpan<ElseSyntax>(t);
            }
        }

        private WhileSyntax ParseWhile()
        {
            using (SpanTracker t = _spans.Create())
            {
                //While condition
                Expect(TokenType.While);
                Expect(TokenType.LeftParen);
                var condition = ParseExpression();
                Expect(TokenType.RightParen);

                Ignore(TokenType.Newline);

                //While body
                BlockSyntax body = null;
                if (Peek(TokenType.LeftBrace)) body = ParseBlock();
                else body = SyntaxFactory.SingleBlock(ParseStatement());

                return SyntaxFactory.While(condition, body).SetSpan<WhileSyntax>(t);
            }
        }

        private ForSyntax ParseFor()
        {
            using (SpanTracker t = _spans.Create())
            {
                //For variable declarations
                Expect(TokenType.For);
                Expect(TokenType.LeftParen);

                IdentifierSyntax iterator = null;
                List<DeclarationSyntax> initializer = new List<DeclarationSyntax>();
                SyntaxNode cond = null;
                List<SyntaxNode> finalizer = new List<SyntaxNode>();

                //For loop can either have the initializer, condition, finalize
                //Or just an iterator variable
                //The iterator variable must be an array and it gets lowered to the same construct
                bool isIterator = !Peek(TokenType.Let);
                if (!isIterator)
                {
                    if (!Peek(TokenType.Colon))
                    {
                        do
                        {
                            var s = ParseDeclaration();
                            if (s != null) initializer.Add(s);
                        } while (PeekAndExpect(TokenType.Comma));
                    }
                    Expect(TokenType.Colon);

                    //For condition
                    cond = ParseExpression();
                    Expect(TokenType.Colon);

                    //For finalize (code that runs at end of loop)
                    if (!Peek(TokenType.RightParen))
                    {
                        do
                        {
                            var s = ParseExpression();
                            if (s != null) finalizer.Add(s);
                        } while (PeekAndExpect(TokenType.Comma));
                    }
                }
                else
                {
                    iterator = ParseArrayAccess();
                }

                Expect(TokenType.RightParen);
                Ignore(TokenType.Newline);

                //For body
                _allowIt = isIterator;
                BlockSyntax body = null;
                if (Peek(TokenType.LeftBrace)) body = ParseBlock();
                else body = SyntaxFactory.SingleBlock(ParseStatement());
                _allowIt = false;

                if(!isIterator)
                {
                    return SyntaxFactory.For(initializer, cond, finalizer, body).SetSpan<ForSyntax>(t);
                }

                return SyntaxFactory.For(iterator, body);
            }
        }

        private SelectSyntax ParseSelect()
        {
            using (SpanTracker t = _spans.Create())
            {
                //Select expression
                Expect(TokenType.Select);
                var e = ParseExpression();
                Ignore(TokenType.Newline);

                Expect(TokenType.LeftBrace);
                IgnoreNewlines();

                //Cases
                List<CaseSyntax> cases = new List<CaseSyntax>();
                while(!Peek(TokenType.RightBrace) && !Peek(TokenType.Else))
                {
                    cases.Add(ParseCase());
                }

                if(Peek(TokenType.Else))
                {
                    cases.Add(ParseDefaultCase());
                }
                Expect(TokenType.RightBrace);

                var s = SyntaxFactory.Select(e, cases).SetSpan<SelectSyntax>(t);
                //Annotations!
                s.Annotation = ParseAnnotation();
                return s;
            }
        }

        private CaseSyntax ParseCase()
        {
            using (SpanTracker t = _spans.Create())
            {
                //Case and expression, we can use "it" to reference the select variable
                _allowIt = true;
                Expect(TokenType.Case);
                List<SyntaxNode> conditions = new List<SyntaxNode>();
                do
                {
                    Ignore(TokenType.Newline);
                    conditions.Add(ParseExpression());
                } while (PeekAndExpect(TokenType.Comma));
                IgnoreNewlines();

                //Statements within the case
                List<SyntaxNode> statements = new List<SyntaxNode>();
                while (!Peek(TokenType.RightBrace) && !Peek(TokenType.Case) && !Peek(TokenType.Else))
                {
                    var s = ParseStatement();
                    if (s != null) statements.Add(s);
                    IgnoreNewlines();
                }
                _allowIt = false;

                return SyntaxFactory.Case(conditions, SyntaxFactory.Block(statements)).SetSpan<CaseSyntax>(t);
            }
        }

        private CaseSyntax ParseDefaultCase()
        {
            using (SpanTracker t = _spans.Create())
            {
                //Else
                _allowIt = true;
                Expect(TokenType.Else);
                IgnoreNewlines();

                //Else statements
                List<SyntaxNode> statements = new List<SyntaxNode>();
                while (!Peek(TokenType.RightBrace))
                {
                    var s = ParseStatement();
                    if (s != null) statements.Add(s);
                    IgnoreNewlines();
                }
                _allowIt = false;

                return SyntaxFactory.Case(new List<SyntaxNode>(), SyntaxFactory.Block(statements)).SetSpan<CaseSyntax>(t);
            }
        }

        #region Expression Parsing
        private SyntaxNode ParseExpression()
        {
            return ParseAssignment();
        }

        private SyntaxNode ParseExpressionWithFullAssignment()
        {
            return ParseFullAssignment();
        }

        private SyntaxNode ParseFullAssignment()
        {
            using (SpanTracker t = _spans.Create())
            {
                //Full assignments allow for multiple variables on the left hand side and also use of the _ token
                //Full assignments are only allowed as separate statements, not in other expressions
                SyntaxNode e = ParseAndOr();
                List<IdentifierSyntax> variables = new List<IdentifierSyntax>();
                if (e is IdentifierSyntax i) variables.Add(i);

                if (PeekAndExpect(TokenType.Comma))
                {
                    do
                    {
                        e = ParseArrayAccess();
                        if (e == null) e = ParseDiscard();
                        if (!(e is IdentifierSyntax ii)) throw ReportError("Only identifiers can be the subject of assignments", e.Span);
                        variables.Add(ii);
                    } while (PeekAndExpect(TokenType.Comma));
                }

                while (PeekAndExpectOneOf(out TokenType tt, TokenType.Equals, TokenType.PlusEquals,
                                                         TokenType.MinusEquals, TokenType.StarEquals,
                                                         TokenType.DivisionEquals, TokenType.ConcatenateEquals))
                {
                    IgnoreNewlines();

                    //new only allowed in assignment and declaration statements
                    SyntaxNode right = ParseExpressionWithFullAssignment();
                    var newStruct = ParseStructInitializer(variables);
                    if (newStruct != null) right = newStruct;

                    if (right == null)
                    {
                        throw ReportError("Expecting expression", t);
                    }

                    e = SyntaxFactory.Assignment(variables, tt.ToAssignmentOperator(), right);
                }

                return e?.SetSpan<SyntaxNode>(t);
            }
        }

        private SyntaxNode ParseAssignment()
        {
            using (SpanTracker t = _spans.Create())
            {
                //Normal assignment only allows one variable
                SyntaxNode e = ParseAndOr();

                while (PeekAndExpectOneOf(out TokenType tt, TokenType.Equals, TokenType.PlusEquals,
                                                           TokenType.MinusEquals, TokenType.StarEquals,
                                                           TokenType.DivisionEquals, TokenType.ConcatenateEquals))
                {
                    IgnoreNewlines();

                    //new only allowed in assignment and declaration statements
                    SyntaxNode right = ParseExpression();
                    if (!(e is IdentifierSyntax i)) throw ReportError("Only identifiers can be the subject of assignment", e.Span);

                    var newStruct = ParseStructInitializer(new List<IdentifierSyntax>() { i });
                    if (newStruct != null) right = newStruct;

                    if (right == null)
                    {
                        ReportError("Expecting expression", t);
                        return null;
                    }

                    e = SyntaxFactory.Assignment(new List<IdentifierSyntax>() { i }, tt.ToAssignmentOperator(), right);
                }

                return e?.SetSpan<SyntaxNode>(t);
            }
        }

        private SyntaxNode ParseAndOr()
        {
            using (SpanTracker t = _spans.Create())
            {
                SyntaxNode e = ParseEquality();

                while (PeekAndExpectOneOf(out TokenType tt, 
                                          TokenType.And, TokenType.Or))
                {
                    IgnoreNewlines();
                    SyntaxNode right = ParseEquality();
                    e = SyntaxFactory.BinaryExpression(e, tt.ToBinaryExpression(), right);
                }

                return e?.SetSpan<SyntaxNode>(t);
            }
        }

        private SyntaxNode ParseEquality()
        {
            using (SpanTracker t = _spans.Create())
            {
                SyntaxNode e = ParseComparison();

                while (PeekAndExpectOneOf(out TokenType tt, 
                                          TokenType.EqualsEquals, TokenType.NotEquals))
                {
                    IgnoreNewlines();
                    SyntaxNode right = ParseComparison();
                    e = SyntaxFactory.BinaryExpression(e, tt.ToBinaryExpression(), right);
                }

                return e?.SetSpan<SyntaxNode>(t);
            }
        }

        private SyntaxNode ParseComparison()
        {
            using (SpanTracker t = _spans.Create())
            {
                SyntaxNode e = ParseAddition();

                while (PeekAndExpectOneOf(out TokenType tt, 
                                 TokenType.LessThan, TokenType.LessThanOrEqual, TokenType.GreaterThan, TokenType.GreaterThanOrEqual))
                {
                    IgnoreNewlines();
                    SyntaxNode right = ParseAddition();
                    e = SyntaxFactory.BinaryExpression(e, tt.ToBinaryExpression(), right);
                }

                return e?.SetSpan<SyntaxNode>(t);
            }
        }

        private SyntaxNode ParseAddition()
        {
            using (SpanTracker t = _spans.Create())
            {
                SyntaxNode e = ParseTerm();

                while (PeekAndExpectOneOf(out TokenType tt, 
                                 TokenType.Plus, TokenType.Minus, TokenType.Concatenate))
                {
                    IgnoreNewlines();
                    SyntaxNode right = ParseTerm();
                    e = SyntaxFactory.BinaryExpression(e, tt.ToBinaryExpression(), right);
                }

                return e?.SetSpan<SyntaxNode>(t);
            }
        }

        private SyntaxNode ParseTerm()
        {
            using (SpanTracker t = _spans.Create())
            {
                SyntaxNode e = ParseUnary();

                while (PeekAndExpectOneOf(out TokenType tt, 
                                 TokenType.Star, TokenType.Division, TokenType.Percent))
                {
                    IgnoreNewlines();
                    SyntaxNode right = ParseUnary();
                    e = SyntaxFactory.BinaryExpression(e, tt.ToBinaryExpression(), right);
                }

                return e?.SetSpan<SyntaxNode>(t);
            }
        }

        private SyntaxNode ParseUnary()
        {
            using (SpanTracker t = _spans.Create())
            {
                //Pre unary operators
                if (PeekAndExpectOneOf(out TokenType tt, TokenType.Bang, TokenType.Minus, 
                                                         TokenType.PlusPlus, TokenType.MinusMinus, TokenType.LengthOf))
                {
                    SyntaxNode right = ParseUnary();
                    return SyntaxFactory.UnaryExpression(right, tt.ToUnaryExpression(true)).SetSpan<SyntaxNode>(t);
                }
                else if(PeekAndExpect(TokenType.StarStar))
                {
                    SyntaxNode right = ParseUnary();
                    return SyntaxFactory.Cast(right).SetSpan<SyntaxNode>(t);
                }
                else if(PeekAndExpect(TokenType.Cast))
                {
                    Expect(TokenType.LeftParen);
                    var type = ParseType();
                    Expect(TokenType.RightParen);

                    SyntaxNode value = ParseUnary();
                    return SyntaxFactory.Cast(value, type).SetSpan<SyntaxNode>(t);
                }

                SyntaxNode left = ParseParen();
                //Post unary operators
                if(PeekAndExpectOneOf(out tt, TokenType.PlusPlus, TokenType.MinusMinus))
                {
                    return SyntaxFactory.UnaryExpression(left, tt.ToUnaryExpression(false)).SetSpan<SyntaxNode>(t);
                }

                return left?.SetSpan<SyntaxNode>(t);
            }
        }

        private SyntaxNode ParseParen()
        {
            using (SpanTracker t = _spans.Create())
            {
                SyntaxNode e = null;
                if (PeekAndExpect(TokenType.LeftParen))
                {
                    e = ParseExpression();
                    Expect(TokenType.RightParen);
                }
                else
                {
                    e = ParseOperand();
                }

                return e?.SetSpan<SyntaxNode>(t);
            }
        }

        private IdentifierSyntax ParseOperand()
        {
            using (SpanTracker t = _spans.Create())
            {
                var e = ParseLiteral();
                if (e == null) e = ParseArrayAccess();
                if (e == null && PeekAndExpect(TokenType.LeftParen))
                {
                    e = ParseExpression();
                    Expect(TokenType.RightParen);
                }
                if (e == null) return null;

                return e.SetSpan<IdentifierSyntax>(t);
            }
        }

        private SyntaxNode ParseLiteral()
        {
            using (SpanTracker t = _spans.Create())
            {
                SyntaxNode e = null;
                if (PeekAndExpect(TokenType.Integer, out string v)) e = SyntaxFactory.NumericLiteral(v, NumberTypes.Integer);
                else if (PeekAndExpect(TokenType.Short, out v)) e = SyntaxFactory.NumericLiteral(v, NumberTypes.Short);
                else if (PeekAndExpect(TokenType.Long, out v)) e = SyntaxFactory.NumericLiteral(v, NumberTypes.Long);
                else if (PeekAndExpect(TokenType.Double, out v)) e = SyntaxFactory.NumericLiteral(v, NumberTypes.Double);
                else if (PeekAndExpect(TokenType.Float, out v)) e = SyntaxFactory.NumericLiteral(v, NumberTypes.Float);
                else if (PeekAndExpect(TokenType.String, out v)) e = SyntaxFactory.StringLiteral(v);
                else if (PeekAndExpect(TokenType.True, out v)) e = SyntaxFactory.BooleanLiteral(v);
                else if (PeekAndExpect(TokenType.False, out v)) e = SyntaxFactory.BooleanLiteral(v);
                else if (_allowIt && PeekAndExpect(TokenType.It)) e = SyntaxFactory.It(); //It only allowed in certain situations
                else if (PeekAndExpect(TokenType.LeftBracket))
                {
                    var type = ParseType();
                    Expect(TokenType.Colon);
                    Expect(TokenType.Integer, out string size);
                    Expect(TokenType.RightBracket);
                    e = SyntaxFactory.ArrayLiteral(type, size);
                }
                else return null;

                return e.SetSpan<SyntaxNode>(t);
            }
        }

        private TypedIdentifierSyntax ParseTypedIdentifier()
        {
            using (SpanTracker t = _spans.Create())
            {
                var fieldType = ParseType();
                Expect(TokenType.Colon);
                Expect(TokenType.Identifier, out string fieldName);
                return SyntaxFactory.TypedIdentifier(fieldType, fieldName).SetSpan<TypedIdentifierSyntax>(t);
            }
        }

        private IdentifierSyntax ParseIdentifier()
        {
            using (SpanTracker t = _spans.Create())
            {
                if (Peek(TokenType.Identifier))
                {
                    Expect(TokenType.Identifier, out string i);
                    return SyntaxFactory.Identifier(i).SetSpan<IdentifierSyntax>(t);
                }
                return null;
            }
        }

        private IdentifierSyntax ParseArrayAccess()
        {
            using (SpanTracker t = _spans.Create())
            {
                var e = ParseMemberAccess();

                if(PeekAndExpect(TokenType.LeftBracket))
                {
                    var index = ParseExpression();
                    if (e == null) ReportError("Expecting expression", t);

                    Expect(TokenType.RightBracket);
                    e = SyntaxFactory.ArrayAccess(e, index);
                }

                return e?.SetSpan<IdentifierSyntax>(t);
            }
        }

        private IdentifierSyntax ParseMemberAccess()
        {
            using (SpanTracker t = _spans.Create())
            {
                IdentifierSyntax e = null;
                if (PeekAndExpect(TokenType.Self))
                {
                    if (!_allowSelf)
                        ReportError("Self only allowed within structs", t);
                    e = SyntaxFactory.Self();
                }
                else if (_stream.Peek(1, out Token tok))
                {
                    switch (tok.Type)
                    {
                        case TokenType.LeftParen:
                            e = ParseMethodCall();
                            break;

                        default:
                            e = ParseIdentifier();
                            break;
                    }
                }

                //Can be null for cases like struct initializers
                if (e == null) return null;

                if (PeekAndExpect(TokenType.Period))
                {
                    var iden = ParseMemberAccess();
                    if (iden != null) e = SyntaxFactory.MemberAccess(e, iden);
                }

                return e.SetSpan<IdentifierSyntax>(t);
            }
        }

        private IdentifierSyntax ParseMethodCall()
        {
            using (SpanTracker t = _spans.Create())
            {
                Expect(TokenType.Identifier, out string pName);
                Expect(TokenType.LeftParen);
                List<SyntaxNode> arguments = new List<SyntaxNode>();
                if (!Peek(TokenType.RightParen))
                {
                    do
                    {
                        var e = ParseExpression();
                        if (e == null) ReportError("Expecting expression", t);
                        else arguments.Add(e);

                    } while (PeekAndExpect(TokenType.Comma));
                }
                Expect(TokenType.RightParen);

                return SyntaxFactory.MethodCall(pName, arguments).SetSpan<MethodCallSyntax>(t);
            }
        }

        private Annotation ParseAnnotation()
        {
            using (SpanTracker t = _spans.Create())
            {
                if (PeekAndExpect(TokenType.Annotation, out string annotation)) return new Annotation(annotation, t);
                return default;
            }
        }
        #endregion

        #region Helper functions
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Expect(TokenType pSymbol)
        {
            if (!_stream.EOF && Current.Type == pSymbol)
            {
                _stream.MoveNext();
            }
            else
            {
                throw ReportError("Expecting " + pSymbol.ToString() + " but encountered " + Current.Type.ToString(), _spans.Current);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Expect(TokenType pSymbol, out string s)
        {
            if (!_stream.EOF && Current.Type == pSymbol)
            {
                string value;
                if (Current.Value.Length == 0) value = _source.Slice(_stream.SourceIndex, Current.Length).ToString();
                else value = Current.Value.ToString();

                _stream.MoveNext();
                s = value;
            }
            else
            {
                throw ReportError("Expecting " + pSymbol.ToString() + " but encountered " + Current.Type.ToString(), _spans.Current);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Ignore(TokenType pSymbol)
        {
            if (Current.Type != TokenType.EndOfFile && Current.Type == pSymbol)
            {
                _stream.MoveNext();
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool Peek(TokenType pSymbol)
        {
            return Current.Type == pSymbol;
        }

        private bool PeekAndExpectOneOf(out TokenType pResult, params TokenType[] pTypes)
        {
            foreach(TokenType t in pTypes)
            {
                if(Peek(t))
                {
                    Expect(t);
                    pResult = t;
                    return true;
                }
            }

            pResult = TokenType.EndOfFile;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool PeekAndExpect(TokenType pSymbol, out string pValue)
        {
            if (Peek(pSymbol))
            {
                Expect(pSymbol, out pValue);
                return true;
            }
            pValue = "";
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool PeekAndExpect(TokenType pSymbol)
        {
            return PeekAndExpect(pSymbol, out string pValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void IgnoreNewlines()
        {
            while (Ignore(TokenType.Newline))
            {
                //Move past all NewLine tokens
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ParseException ReportError(string pError, TextSpan pSpan)
        {
            _error.WriteError(pError, pSpan);
            return new ParseException(pError);
        }

        private void Synchronize()
        {
            _stream.MoveNext();
            while (!Peek(TokenType.EndOfFile))
            {
                switch (_stream.Current.Type)
                {
                    case TokenType.Newline:
                        _stream.MoveNext();
                        return;

                    case TokenType.Let:
                    case TokenType.If:
                    case TokenType.While:
                    case TokenType.For:
                    case TokenType.RightBrace:
                    case TokenType.Select:
                        //Do nothing
                        return;
                }

                _stream.MoveNext();
            }
        }
        #endregion  
    }
}
