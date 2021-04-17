using System;
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
            // if (char.IsLetter(_lastCharacter) || _lastCharacter == '_')
            // {
            //     token = ParseIdentifierOrType();
            // }
                
            Thread.Sleep(100);
            return new Token.Token(TokenType.Unknown, _lastCharacter.ToString());
        }

        private Token.Token ParseIdentifierOrWordToken()
        {
            switch (_lastCharacter)
            {
                case 'i':
                    return TryParseSequence("int") ? new Token.Token(TokenType.Int) : ParseIdentifier();
                case 'f':
                    return TryParseSequence("float") ? new Token.Token(TokenType.Float) : ParseIdentifier();
                case 'r':
                    return TryParseSequence("return") ? new Token.Token(TokenType.Return) : ParseIdentifier();
                case 's':
                    return TryParseSequence("str") ? new Token.Token(TokenType.String) : ParseIdentifier();
                case 'b':
                    return TryParseSequence("bool") ? new Token.Token(TokenType.Bool) : ParseIdentifier();
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
                _tokenValue = (string)_tokenValue + _lastCharacter;
            }

            return true;
        }

        private Token.Token ParseIdentifier()
        {
            throw new NotImplementedException();
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