using System;
using Serilog;
using Translator.Token;
using static Translator.Token.TokenType;

namespace Translator
{
    public class Lexer
    {
        private readonly ICharacterSource _source;
        private char _lastCharacter;
        private bool _sourceEnd;
        private TokenValue _tokenValue;

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
            Log.Debug($"GetChar fetched character: {c}");
            _lastCharacter = c.GetValueOrDefault();
        }

        private bool NextToken()
        {
            return _sourceEnd ||_lastCharacter == ' ' || _lastCharacter == '\n'
                   || _lastCharacter == '(' || _lastCharacter == ')' || _lastCharacter == ':'
                   || _lastCharacter == '\r';
        }

        public Token.Token GetNextToken()
        {
            _tokenValue = new TokenValue();
            while (_lastCharacter == ' ')
                GetChar();
            if (_lastCharacter == '#')
                SkipCommentLine();
            if (_sourceEnd)
                return new Token.Token(End);
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

        private Token.Token ParseStringLiteral()
        {
            _tokenValue.SetString("");
            GetChar();
            while (_lastCharacter != '\"')
            {
                if (_sourceEnd)
                    return new Token.Token(Unknown);
                _tokenValue.ConcatString(_lastCharacter.ToString());
                GetChar(); 
            }
            return new Token.Token(StringLiteral, _tokenValue);
        }

        private Token.Token ParseNumericConstant()
        {
            _tokenValue.SetInt(0);
            if (_lastCharacter == '0')
            {
                GetChar();
                if (_lastCharacter == '.')
                    return ParseDecimalConstant();
                if (NextToken())
                    return new Token.Token(IntConstant, new TokenValue(0));
                return new Token.Token(Unknown);
            }
            return ParseIntegerConstant();
        }

        private Token.Token ParseIntegerConstant()
        {
            while (!NextToken())
            {
                _tokenValue.SetInt(_tokenValue.GetInt() * 10 + (_lastCharacter - '0'));
                GetChar();
                if (!char.IsDigit(_lastCharacter) && !NextToken())
                {
                    if (_lastCharacter == '.')
                        return ParseDecimalConstant();
                    return new Token.Token(Unknown);
                }
            }
            return new Token.Token(IntConstant, _tokenValue);
        }

        private Token.Token ParseDecimalConstant()
        {
            _tokenValue.ConvertToDouble();
            GetChar();
            int i = 1;
            while (!NextToken())
            {
                _tokenValue.AddDouble((_lastCharacter - '0') / Math.Pow(10.0, i++));
                GetChar();
                if (!char.IsDigit(_lastCharacter) && !NextToken())
                    return new Token.Token(Unknown);
            }
            return new Token.Token(DecimalConstant, _tokenValue);
        }

        private Token.Token ParseIdentifierOrWordTokenOrLogicalConstant()
        {
            switch (_lastCharacter)
            {
                case 'i':
                    _tokenValue.ConcatString(_lastCharacter.ToString());
                    GetChar();
                    if (_lastCharacter == 'f')
                        return TryParseSequence("f") ? new Token.Token(If) : ParseIdentifier();
                    else
                        return TryParseSequence("nt") ? new Token.Token(Int) : ParseIdentifier();
                case 'f':
                    _tokenValue.ConcatString(_lastCharacter.ToString());
                    GetChar();
                    if (_lastCharacter == 'l')
                        return TryParseSequence("loat") ? new Token.Token(Float) : ParseIdentifier();
                    else
                        return TryParseSequence("or") ? new Token.Token(For) : ParseIdentifier();
                case 'F':
                    return TryParseSequence("False") ? new Token.Token(LogicalConstant, new TokenValue(false)) : ParseIdentifier();
                case 'r':
                    return TryParseSequence("return") ? new Token.Token(Return) : ParseIdentifier();
                case 's':
                    return TryParseSequence("str") ? new Token.Token(TokenType.String) : ParseIdentifier();
                case 'b':
                    return TryParseSequence("bool") ? new Token.Token(Bool) : ParseIdentifier();
                case 'd':
                    return TryParseSequence("def") ? new Token.Token(Def) : ParseIdentifier();
                case 'n':
                    return TryParseSequence("not") ? new Token.Token(Not) : ParseIdentifier();
                case 'o':
                    return TryParseSequence("or") ? new Token.Token(Or) : ParseIdentifier();
                case 'a':
                    return TryParseSequence("and") ? new Token.Token(And) : ParseIdentifier();
                case 'w':
                    return TryParseSequence("while") ? new Token.Token(While) : ParseIdentifier();
                case 'T':
                    return TryParseSequence("True") ? new Token.Token(LogicalConstant, new TokenValue(true)) : ParseIdentifier();
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
                if (!NextToken())
                    _tokenValue.ConcatString(_lastCharacter.ToString());
                GetChar();
            }
            return NextToken();
        }

        private Token.Token ParseIdentifier()
        {
            if (char.IsDigit(_lastCharacter))
                return new Token.Token(Unknown);
            while (!NextToken())
            {
                if (!char.IsLetter(_lastCharacter) && !char.IsDigit(_lastCharacter) && _lastCharacter != '_')
                    return new Token.Token(Unknown);
                _tokenValue.ConcatString(_lastCharacter.ToString());
                GetChar();
            }
            return new Token.Token(Identifier, _tokenValue);
        }

        private Token.Token GetCharAndReturnToken(TokenType tokenType)
        {
            GetChar();
            return new Token.Token(tokenType);
        }

        private Token.Token? ParseSpecialCharacterSymbol()
        {
            switch (_lastCharacter)
            {
                case '(':
                    return GetCharAndReturnToken(LeftParenthesis);
                case ')':
                    return GetCharAndReturnToken(RightParenthesis);
                case ':':
                    return GetCharAndReturnToken(Colon);
                case ',':
                    return GetCharAndReturnToken(Comma);
                case '+':
                    return GetCharAndReturnToken(Plus);
                case '-':
                    GetChar();
                    return _lastCharacter == '>' ? GetCharAndReturnToken(Arrow) : GetCharAndReturnToken(Minus);
                case '*':
                    return GetCharAndReturnToken(Star);
                case '/':
                    return GetCharAndReturnToken(Slash);
                case '=':
                    GetChar();
                    return _lastCharacter == '=' ? GetCharAndReturnToken(TokenType.Equals) : new Token.Token(Assignment);
                case '<':
                    GetChar();
                    return _lastCharacter == '=' ? GetCharAndReturnToken(LessEqualThan) : new Token.Token(LessThan);
                case '>':
                    GetChar();
                    return _lastCharacter == '=' ? GetCharAndReturnToken(GreaterEqualThan) : new Token.Token(GreaterThan);
                case '!':
                    GetChar();
                    return _lastCharacter == '='
                        ? GetCharAndReturnToken(NotEquals)
                        : GetCharAndReturnToken(Unknown);
                case '\t':
                    return GetCharAndReturnToken(Indent);
                case '\n':
                    return GetCharAndReturnToken(Newline);
                case '\r':
                    GetChar();
                    if (_lastCharacter == '\n')
                        return GetCharAndReturnToken(Newline);
                    return new Token.Token(Unknown);
                default:
                    return GetCharAndReturnToken(Unknown);
            }
        }

        private void SkipCommentLine()
        {
            while (_lastCharacter != '\n' && !_sourceEnd) 
                GetChar();
            GetChar();
        }
    }
}