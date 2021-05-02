using System;
using PythonCSharpTranslator;
using PythonCSharpTranslator.Exception;

namespace Translator.Token
{
    public class Token
    {
        public TokenType Type { get; set; }

        public TokenValue Value { get; set; }
        
        public int LineNumber { get; set; }
        public int ColumnNumber { get; set; }

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

        public Token(TokenType type, TokenValue value, int lineNumber, int columnNumber)
        {
            Type = type;
            Value = value;
            LineNumber = lineNumber;
            ColumnNumber = columnNumber;
        }

        public override string ToString()
        {
            return $"{Type}:{Value}";
        }
    }
}