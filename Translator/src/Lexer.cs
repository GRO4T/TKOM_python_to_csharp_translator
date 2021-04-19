using System;
using Serilog;
using Translator.Token;

namespace Translator
{
    public class Lexer
    {
        private ICharacterSource _source;
        private char _lastCharacter;
        private bool _sourceEnd;
        private object _tokenValue;

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
            return _sourceEnd || _lastCharacter == ' ' || _lastCharacter == '\n';
        }

        public Token.Token GetNextToken()
        {
            _tokenValue = _lastCharacter.ToString(); 
            if (_sourceEnd)
                return new Token.Token(TokenType.End);
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
            throw new NotImplementedException();
        }

        private Token.Token ParseNumericConstant()
        {
            Log.Debug(_tokenValue.ToString());
            if (_lastCharacter == '0')
            {
                GetChar();
                if (_lastCharacter == '.')
                    return ParseDecimalConstant();
                if (NextToken())
                    return new Token.Token(TokenType.IntConstant, 0);
                return new Token.Token(TokenType.Unknown);
            }
            return ParseIntegerConstant();
        }

        private Token.Token ParseIntegerConstant()
        {
            _tokenValue = 0;
            while (!NextToken())
            {
                _tokenValue = (int) _tokenValue * 10 + (_lastCharacter - '0');
                if (!char.IsDigit(_lastCharacter))
                {
                    if (_lastCharacter == '.')
                        return ParseDecimalConstant();
                    return new Token.Token(TokenType.Unknown);
                }
                GetChar();
            }
            return new Token.Token(TokenType.IntConstant, _tokenValue);
        }

        private Token.Token ParseDecimalConstant()
        {
            _tokenValue = 0.0;
            GetChar();
            int i = 1;
            while (!NextToken())
            {
                _tokenValue =  (double)_tokenValue + (_lastCharacter - '0') / Math.Pow(10.0, i++);
                if (!char.IsDigit(_lastCharacter))
                    return new Token.Token(TokenType.Unknown);
                GetChar();
            }
            return new Token.Token(TokenType.DecimalConstant, _tokenValue);
        }

        private Token.Token ParseIdentifierOrWordTokenOrLogicalConstant()
        {
            switch (_lastCharacter)
            {
                case 'i':
                    GetChar();
                    _tokenValue = (string)_tokenValue + _lastCharacter;
                    if (_lastCharacter == 'f')
                        return TryParseSequence("f") ? new Token.Token(TokenType.If) : ParseIdentifier();
                    else
                        return TryParseSequence("nt") ? new Token.Token(TokenType.Int) : ParseIdentifier();
                case 'f':
                    GetChar();
                    _tokenValue = (string)_tokenValue + _lastCharacter;
                    if (_lastCharacter == 'l')
                        return TryParseSequence("loat") ? new Token.Token(TokenType.Float) : ParseIdentifier();
                    else if (_lastCharacter == 'a')
                        return TryParseSequence("alse") ? new Token.Token(TokenType.LogicalConstant, false) : ParseIdentifier();
                    else
                        return TryParseSequence("or") ? new Token.Token(TokenType.For) : ParseIdentifier();
                case 'r':
                    return TryParseSequence("return") ? new Token.Token(TokenType.Return) : ParseIdentifier();
                case 's':
                    return TryParseSequence("str") ? new Token.Token(TokenType.String) : ParseIdentifier();
                case 'b':
                    return TryParseSequence("bool") ? new Token.Token(TokenType.Bool) : ParseIdentifier();
                case 'd':
                    return TryParseSequence("def") ? new Token.Token(TokenType.Def) : ParseIdentifier();
                case 'n':
                    return TryParseSequence("not") ? new Token.Token(TokenType.Not) : ParseIdentifier();
                case 'o':
                    return TryParseSequence("or") ? new Token.Token(TokenType.Or) : ParseIdentifier();
                case 'a':
                    return TryParseSequence("and") ? new Token.Token(TokenType.And) : ParseIdentifier();
                case 'w':
                    return TryParseSequence("while") ? new Token.Token(TokenType.While) : ParseIdentifier();
                case 't':
                    return TryParseSequence("true") ? new Token.Token(TokenType.LogicalConstant, true) : ParseIdentifier();
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
                GetChar();
                if (!NextToken())
                    _tokenValue = (string)_tokenValue + _lastCharacter;
            }
            Log.Debug("(" + _tokenValue.ToString() + ")");
            return (_lastCharacter == 0 || _sourceEnd || _lastCharacter == '\n');
        }

        private Token.Token ParseIdentifier()
        {
            if (char.IsDigit(_lastCharacter))
                return new Token.Token(TokenType.Unknown);
            while (!NextToken())
            {
                if (!char.IsLetter(_lastCharacter) && !char.IsDigit(_lastCharacter) && _lastCharacter != '_')
                    return new Token.Token(TokenType.Unknown);
                GetChar();
                _tokenValue = (string)_tokenValue + _lastCharacter;
            }
            return new Token.Token(TokenType.Identifier, _tokenValue);
        }

        private Token.Token? ParseSpecialCharacterSymbol()
        {
            switch (_lastCharacter)
            {
                case '(':
                    return new Token.Token(TokenType.LeftParenthesis);
                case ')':
                    return new Token.Token(TokenType.RightParenthesis);
                case ':':
                    return new Token.Token(TokenType.Colon);
                case ',':
                    return new Token.Token(TokenType.Comma);
                case '+':
                    return new Token.Token(TokenType.Plus);
                case '-':
                    return new Token.Token(TokenType.Minus);
                case '*':
                    return new Token.Token(TokenType.Star);
                case '/':
                    return new Token.Token(TokenType.Slash);
                case '=':
                    GetChar();
                    return _lastCharacter == '=' ? new Token.Token(TokenType.Equals) : new Token.Token(TokenType.Assignment);
                case '<':
                    GetChar();
                    return _lastCharacter == '=' ? new Token.Token(TokenType.LessEqualThan) : new Token.Token(TokenType.LessThan);
                case '>':
                    GetChar();
                    return _lastCharacter == '=' ? new Token.Token(TokenType.GreaterEqualThan) : new Token.Token(TokenType.GreaterThan);
                case '!':
                    GetChar();
                    return _lastCharacter == '='
                        ? new Token.Token(TokenType.NotEquals)
                        : new Token.Token(TokenType.Unknown);
                default:
                    return new Token.Token(TokenType.Unknown);
            }
        }
    }
}