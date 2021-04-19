using System;
using System.Diagnostics;
using System.Threading;
using PythonCSharpTranslator;
using Serilog;
using Translator.Token;

namespace Translator
{
    public class Lexer
    {
        private ICharacterSource _source;
        private char _lastCharacter;
        private bool _sourceEnd = false;
        private object _tokenValue;

        public Lexer(ICharacterSource characterSource)
        {
            _source = characterSource;
            GetChar();
        }

        private char? GetChar()
        {
            char? c = _source.GetChar();
            if (c == null)
                _sourceEnd = true;
            Log.Information($"Lexer fetched character: {c}");
            _lastCharacter = c.GetValueOrDefault();
            return c;
        }

        public Token.Token GetNextToken()
        {
            _tokenValue = null;
            Token.Token? token;
            if (_sourceEnd)
                return new Token.Token(TokenType.End);
            if ((token = ParseSpecialCharacterSymbol()) != null)
                return token;
            if (_lastCharacter == '_')
            {
                _tokenValue = "_";
                return ParseIdentifier();
            }
            if (char.IsLetter(_lastCharacter))
            {
                _tokenValue = _lastCharacter.ToString(); 
                return ParseIdentifierOrWordToken();
            }
            return new Token.Token(TokenType.Unknown, _lastCharacter.ToString());
        }

        private Token.Token ParseIdentifierOrWordToken()
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
                if (character != seq[^1])
                    _tokenValue = (string)_tokenValue + _lastCharacter;
            }
            Log.Debug("(" + _tokenValue.ToString() + ")");
            return (_lastCharacter == 0 || _sourceEnd || _lastCharacter == '\n');
        }

        private Token.Token ParseIdentifier()
        {
            while (_lastCharacter != 0 && _lastCharacter != '\n' && !_sourceEnd)
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
                    return null;
            }
        }
    }
}