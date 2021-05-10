using System;
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

        private bool NextTokenSymbol()
        {
            return _sourceEnd ||_lastCharacter == ' ' || _lastCharacter == '\n'
                   || _lastCharacter == '(' || _lastCharacter == ')' || _lastCharacter == ':'
                   || _lastCharacter == '\r';
        }

        public Token GetNextToken()
        {
            _tokenValue = new TokenValue();
            while (_lastCharacter == ' ')
                GetChar();
            if (_lastCharacter == '#')
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

        private Token ParseStringLiteral()
        {
            _tokenValue.SetString("");
            GetChar();
            while (_lastCharacter != '\"')
            {
                if (_sourceEnd)
                    return CreateToken(Unknown);
                _tokenValue.ConcatString(_lastCharacter.ToString());
                GetChar(); 
            }
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
                if (NextTokenSymbol())
                    return CreateToken(IntegerConstant, new TokenValue(0));
                return CreateToken(Unknown);
            }
            return ParseIntegerConstant();
        }

        private Token ParseIntegerConstant()
        {
            while (!NextTokenSymbol())
            {
                _tokenValue.SetInt(_tokenValue.GetInt() * 10 + (_lastCharacter - '0'));
                GetChar();
                if (!char.IsDigit(_lastCharacter) && !NextTokenSymbol())
                {
                    if (_lastCharacter == '.')
                        return ParseDecimalConstant();
                    return CreateToken(Unknown);
                }
            }
            return CreateToken(IntegerConstant, _tokenValue);
        }

        private Token ParseDecimalConstant()
        {
            _tokenValue.ConvertToDouble();
            GetChar();
            int i = 1;
            while (!NextTokenSymbol())
            {
                _tokenValue.AddDouble((_lastCharacter - '0') / Math.Pow(10.0, i++));
                GetChar();
                if (!char.IsDigit(_lastCharacter) && !NextTokenSymbol())
                    return CreateToken(Unknown);
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
                        return TryParseSequence("f") ? CreateToken(If) : ParseIdentifier();
                    else
                        return TryParseSequence("nt") ? CreateToken(IntegerType) : ParseIdentifier();
                case 'f':
                    _tokenValue.ConcatString(_lastCharacter.ToString());
                    GetChar();
                    if (_lastCharacter == 'l')
                        return TryParseSequence("loat") ? CreateToken(DecimalType) : ParseIdentifier();
                    else
                        return TryParseSequence("or") ? CreateToken(For) : ParseIdentifier();
                case 'r':
                    return TryParseSequence("return") ? CreateToken(Return) : ParseIdentifier();
                case 's':
                    return TryParseSequence("str") ? CreateToken(StringType) : ParseIdentifier();
                case 'b':
                    return TryParseSequence("bool") ? CreateToken(BooleanType) : ParseIdentifier();
                case 'd':
                    return TryParseSequence("def") ? CreateToken(Def) : ParseIdentifier();
                case 'n':
                    return TryParseSequence("not") ? CreateToken(Not) : ParseIdentifier();
                case 'o':
                    return TryParseSequence("or") ? CreateToken(Or) : ParseIdentifier();
                case 'a':
                    return TryParseSequence("and") ? CreateToken(And) : ParseIdentifier();
                case 'w':
                    return TryParseSequence("while") ? CreateToken(While) : ParseIdentifier();
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
                if (!NextTokenSymbol())
                    _tokenValue.ConcatString(_lastCharacter.ToString());
                GetChar();
            }
            return NextTokenSymbol();
        }

        private Token ParseIdentifier()
        {
            if (char.IsDigit(_lastCharacter))
                return CreateToken(Unknown);
            while (!NextTokenSymbol())
            {
                if (!char.IsLetter(_lastCharacter) && !char.IsDigit(_lastCharacter) && _lastCharacter != '_')
                    return CreateToken(Unknown);
                _tokenValue.ConcatString(_lastCharacter.ToString());
                GetChar();
            }

            return CreateToken(Identifier, _tokenValue);
        }

        private Token GetCharAndReturnToken(TokenType tokenType)
        {
            GetChar();
            return CreateToken(tokenType);
        }

        private Token? ParseSpecialCharacterSymbol()
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
                    return _lastCharacter == '=' ? GetCharAndReturnToken(TokenType.EqualSymbol) : CreateToken(AssignmentSymbol);
                case '<':
                    GetChar();
                    return _lastCharacter == '=' ? GetCharAndReturnToken(LessEqualThan) : CreateToken(LessThan);
                case '>':
                    GetChar();
                    return _lastCharacter == '=' ? GetCharAndReturnToken(GreaterEqualThan) : CreateToken(GreaterThan);
                case '!':
                    GetChar();
                    return _lastCharacter == '='
                        ? GetCharAndReturnToken(NotEqualSymbol)
                        : GetCharAndReturnToken(Unknown);
                case '\t':
                    return GetCharAndReturnToken(Indent);
                case '\n':
                    return GetCharAndReturnToken(Newline);
                case '\r':
                    GetChar();
                    if (_lastCharacter == '\n')
                        return GetCharAndReturnToken(Newline);
                    return CreateToken(Unknown);
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

        private Token CreateToken(TokenType type, TokenValue value = null)
        {
            return new(type, value, _source.GetLineNumber(), _source.GetColumnNumber() - 1);
        }
    }
}