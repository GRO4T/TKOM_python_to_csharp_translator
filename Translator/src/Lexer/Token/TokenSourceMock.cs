namespace PythonCSharpTranslator
{
    public class TokenSourceMock : ITokenSource
    {
        private Token[] _tokens;
        private int _currTokenIndex = 0;

        TokenSourceMock(Token[] tokens)
        {
            _tokens = tokens; 
        }
        public Token GetNextToken()
        {
            return _currTokenIndex < _tokens.Length ? _tokens[_currTokenIndex++] : new Token(TokenType.End);
        }
        
    }
}