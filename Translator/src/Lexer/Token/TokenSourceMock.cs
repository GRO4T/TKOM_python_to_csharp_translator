using System.Collections.Generic;

namespace PythonCSharpTranslator
{
    public class TokenSourceMock : ITokenSource
    {
        private List<Token> _tokens;
        private int _currTokenIndex = 0;

        public TokenSourceMock(List<Token> tokens)
        {
            _tokens = tokens; 
        }
        public Token GetNextToken()
        {
            return _currTokenIndex < _tokens.Count ? _tokens[_currTokenIndex++] : new Token(TokenType.End);
        }

        public int GetLineNumber()
        {
            throw new System.NotImplementedException();
        }
    }
}