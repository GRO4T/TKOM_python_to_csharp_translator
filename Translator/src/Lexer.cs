using System;
using System.Diagnostics;
using System.Threading;
using Serilog;
using Translator;

namespace PythonCSharpTranslator
{
    public class Lexer
    {
        private CharacterSource _source;

        public Lexer()
        {
            _source = new CharacterSource("Resources/input.py");
        }

        public Token GetNextToken()
        {
            char? c = _source.GetChar();
            Log.Information($"Lexer fetched character: {c}");
            if (c == null)
                return new Token(TokenType.End);
            Thread.Sleep(100);
            return new Token(TokenType.Identifier, c.ToString());
        }
    }

    internal class ArrayList<T>
    {
    }
}