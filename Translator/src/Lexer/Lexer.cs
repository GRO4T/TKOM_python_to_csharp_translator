using System;
using System.Collections;
using Serilog;
using static PythonCSharpTranslator.TokenType;

namespace PythonCSharpTranslator
{
    public class Lexer : ITokenSource
    {
        private readonly ICharacterSource _source;
        private char _lastCharacter;
        private bool _sourceEnd;
        private TokenValue _tokenValue;
        private bool _parsingIndent = true;
        private const int IndentWidth = 4;
        private int _indentCounter = 0;

        public Lexer(ICharacterSource characterSource)
        {
            _source = characterSource;
            GetChar();
        }

        private void GetChar()
        {
            char? c = _source.GetChar();
            if (c == null)
                _sourceEnd = true;
            if (_parsingIndent && c != '\t' && c != ' ') _parsingIndent = false;
            Log.Debug($"GetChar fetched character: {c}");
            _lastCharacter = c.GetValueOrDefault();
        }

        private bool IsTokenSeparator()
        {
            return _sourceEnd || 
                   ((IList) new[] {' ', '\n', '(', ')', ':', '\r', ',', '+', '-', '*', '/', '=', '!'}).Contains(_lastCharacter);
        }

        public int GetLineNumber()
        {
            return _source.GetLineNumber();
        }

        public Token GetNextToken()
        {
            _tokenValue = new TokenValue();
            var token = ParseIndentOrSkipWhites();
            if (token != null) return token;
            while (_lastCharacter == '#')
                SkipCommentLine();
            if (_sourceEnd)
                return CreateToken(End);
            if (char.IsDigit(_lastCharacter))
                return ParseNumericConstant();
            if (_lastCharacter == '"')
                return ParseStringLiteral();
            if (_lastCharacter == '_')
                return ParseIdentifier();
            if (char.IsLetter(_lastCharacter))
                return ParseIdentifierOrWordTokenOrLogicalConstant();
            return ParseSpecialCharacterSymbol();
        }

        private Token? ParseIndentOrSkipWhites()
        {
            while (_lastCharacter == ' ')
            {
                if (_parsingIndent)
                {
                    if (++_indentCounter == IndentWidth)
                    {
                        _indentCounter = 0;
                        GetChar();
                        return CreateToken(TabToken);
                    }
                }
                GetChar();
            }
            return _indentCounter != 0 ? CreateUnknownToken("Indentation error!") : null;
        }

        private Token ParseStringLiteral()
        {
            _tokenValue.SetString("");
            GetChar();
            while (_lastCharacter != '\"')
            {
                if (_sourceEnd)
                    return CreateToken(UnknownToken);
                _tokenValue.ConcatString(_lastCharacter.ToString());
                GetChar(); 
            }
            GetChar();
            return CreateToken(StringLiteral, _tokenValue);
        }

        private Token ParseNumericConstant()
        {
            _tokenValue.SetInt(0);
            if (_lastCharacter == '0')
            {
                GetChar();
                if (_lastCharacter == '.')
                    return ParseDecimalConstant();
                if (IsTokenSeparator())
                    return CreateToken(IntegerConstant, new TokenValue(0));
                return CreateToken(UnknownToken);
            }
            return ParseIntegerConstant();
        }

        private Token ParseIntegerConstant()
        {
            while (!IsTokenSeparator())
            {
                _tokenValue.SetInt(_tokenValue.GetInt() * 10 + (_lastCharacter - '0'));
                GetChar();
                if (!char.IsDigit(_lastCharacter) && !IsTokenSeparator())
                {
                    if (_lastCharacter == '.')
                        return ParseDecimalConstant();
                    return CreateToken(UnknownToken);
                }
            }
            return CreateToken(IntegerConstant, _tokenValue);
        }

        private Token ParseDecimalConstant()
        {
            _tokenValue.ConvertToDouble();
            GetChar();
            int i = 1;
            while (!IsTokenSeparator())
            {
                _tokenValue.AddDouble((_lastCharacter - '0') / Math.Pow(10.0, i++));
                GetChar();
                if (!char.IsDigit(_lastCharacter) && !IsTokenSeparator())
                    return CreateToken(UnknownToken);
            }
            return CreateToken(DecimalConstant, _tokenValue);
        }

        private Token ParseIdentifierOrWordTokenOrLogicalConstant()
        {
            switch (_lastCharacter)
            {
                case 'i':
                    _tokenValue.ConcatString(_lastCharacter.ToString());
                    GetChar();
                    if (_lastCharacter == 'f')
                        return TryParseSequence("f") ? CreateToken(IfToken) : ParseIdentifier();
                    else if (_lastCharacter == 'n')
                    {
                        GetChar();
                        if (IsTokenSeparator())
                            return CreateToken(InToken);
                        if (_lastCharacter == 't')
                        {
                            GetChar();
                            if (IsTokenSeparator())
                                return CreateToken(IntToken);
                        }
                    }
                    return ParseIdentifier();
                case 'f':
                    _tokenValue.ConcatString(_lastCharacter.ToString());
                    GetChar();
                    if (_lastCharacter == 'l')
                        return TryParseSequence("loat") ? CreateToken(FloatToken) : ParseIdentifier();
                    return TryParseSequence("or") ? CreateToken(ForToken) : ParseIdentifier();
                case 'r':
                    GetChar();
                    if (_lastCharacter == 'a')
                        return TryParseSequence("ange") ? CreateToken(RangeToken) : ParseIdentifier();
                    return TryParseSequence("eturn") ? CreateToken(Return) : ParseIdentifier();
                case 's':
                    return TryParseSequence("str") ? CreateToken(StrToken) : ParseIdentifier();
                case 'b':
                    return TryParseSequence("bool") ? CreateToken(BoolToken) : ParseIdentifier();
                case 'd':
                    return TryParseSequence("def") ? CreateToken(DefToken) : ParseIdentifier();
                case 'n':
                    return TryParseSequence("not") ? CreateToken(NotToken, new TokenValue("!")) : ParseIdentifier();
                case 'o':
                    return TryParseSequence("or") ? CreateToken(OrToken, new TokenValue("||")) : ParseIdentifier();
                case 'a':
                    return TryParseSequence("and") ? CreateToken(AndToken, new TokenValue("&&")) : ParseIdentifier();
                case 'w':
                    return TryParseSequence("while") ? CreateToken(WhileToken) : ParseIdentifier();
                case 'T':
                    return TryParseSequence("True") ? CreateToken(LogicalConstant, new TokenValue(true)) : ParseIdentifier();
                case 'F':
                    return TryParseSequence("False") ? CreateToken(LogicalConstant, new TokenValue(false)) : ParseIdentifier();
                default:
                    return ParseIdentifier();
            }
        }

        private bool TryParseSequence(string seq)
        {
            foreach (char character in seq)
            {
                if (_lastCharacter != character)
                    return false;
                if (!IsTokenSeparator())
                    _tokenValue.ConcatString(_lastCharacter.ToString());
                GetChar();
            }
            return IsTokenSeparator();
        }

        private Token ParseIdentifier()
        {
            if (char.IsDigit(_lastCharacter))
                return CreateToken(UnknownToken);
            while (!IsTokenSeparator())
            {
                if (!char.IsLetter(_lastCharacter) && !char.IsDigit(_lastCharacter) && _lastCharacter != '_')
                    return CreateToken(UnknownToken);
                _tokenValue.ConcatString(_lastCharacter.ToString());
                GetChar();
            }

            return CreateToken(Identifier, _tokenValue);
        }

        private Token GetCharAndReturnToken(TokenType tokenType, string value = null)
        {
            GetChar();
            return CreateToken(tokenType, new TokenValue(value));
        }

        private Token? ParseSpecialCharacterSymbol()
        {
            switch (_lastCharacter)
            {
                case '(':
                    return GetCharAndReturnToken(LeftParenthesis, "(");
                case ')':
                    return GetCharAndReturnToken(RightParenthesis, ")");
                case ':':
                    return GetCharAndReturnToken(Colon, ":");
                case ',':
                    return GetCharAndReturnToken(Comma, ",");
                case '+':
                    return GetCharAndReturnToken(Plus, "+");
                case '-':
                    GetChar();
                    return _lastCharacter == '>' ? GetCharAndReturnToken(Arrow, "->") : GetCharAndReturnToken(Minus, "-");
                case '*':
                    return GetCharAndReturnToken(Star, "*");
                case '/':
                    return GetCharAndReturnToken(Slash, "/");
                case '=':
                    GetChar();
                    return _lastCharacter == '=' ? GetCharAndReturnToken(EqualSymbol, "==") : CreateToken(AssignmentSymbol, new TokenValue("="));
                case '<':
                    GetChar();
                    return _lastCharacter == '=' ? GetCharAndReturnToken(LessEqualThan, "<=") : CreateToken(LessThan, new TokenValue("<"));
                case '>':
                    GetChar();
                    return _lastCharacter == '=' ? GetCharAndReturnToken(GreaterEqualThan, ">=") : CreateToken(GreaterThan, new TokenValue(">"));
                case '!':
                    GetChar();
                    return _lastCharacter == '='
                        ? GetCharAndReturnToken(NotEqualSymbol, "!=")
                        : GetCharAndReturnToken(UnknownToken);
                case '\t':
                    return GetCharAndReturnToken(TabToken);
                case '\n':
                    _indentCounter = 0;
                    _parsingIndent = true;
                    return GetCharAndReturnToken(NewlineToken);
                case '\r':
                    GetChar();
                    if (_lastCharacter == '\n')
                    {
                        _indentCounter = 0;
                        _parsingIndent = true;
                        return GetCharAndReturnToken(NewlineToken);
                    }
                    return CreateToken(UnknownToken);
                default:
                    return GetCharAndReturnToken(UnknownToken);
            }
        }
        
        private void SkipCommentLine()
        {
            while (_lastCharacter != '\n' && !_sourceEnd) 
                GetChar();
            GetChar();
        }

        private Token CreateToken(TokenType type, TokenValue value = null)
        {
            return new()
            {
                Type = type,
                Value = value,
                LineNumber = _source.GetLineNumber(),
                ColumnNumber = _source.GetColumnNumber() - 1,
            };
        }

        private Token CreateUnknownToken(string errorDescription = null)
        {
            return new()
            {
                Type = UnknownToken,
                LineNumber = _source.GetLineNumber(),
                ColumnNumber = _source.GetColumnNumber() - 1,
                ErrorDescription = errorDescription
            };
        }
    }
}