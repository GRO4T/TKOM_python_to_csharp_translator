using System;
using PythonCSharpTranslator;
using PythonCSharpTranslator.Exception;

namespace Translator.Token
{
    public class Token
    {
        public TokenType Type { get; set; }

        public TokenValue Value { get; set; }

        public Token()
        {
            
        }

        public Token(TokenType type)
        {
            Type = type;
        }
        public Token(TokenType type, TokenValue value)
        {
            Type = type;
            Value = value;
        }

        public override string ToString()
        {
            return $"{Type}:{Value}";
        }
    }
}