using PythonCSharpTranslator;
using Serilog;
using Translator.CharacterSource;
using Translator.Token;

namespace Translator
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File("logs\\logfile_default.txt")
                .CreateLogger();

            // Lexer lexer = new Lexer(new StringCharacterSource("test_integer = int(3)"));
            // Lexer lexer = new Lexer(new StringCharacterSource("intValue = 1"));
            Lexer lexer = new Lexer(new FileCharacterSource("Resources/input.py"));
            Token.Token token = lexer.GetNextToken();
            while (token.Type != TokenType.End)
            {
                if (token.Type == TokenType.Unknown)
                    Log.Information($"Parser fetched token: {token} line:{token.LineNumber} column:{token.ColumnNumber}");
                else
                    Log.Information($"Parser fetched token: {token}");
                token = lexer.GetNextToken();
            }
            Log.Information($"Parser fetched token: {token}");

            Log.CloseAndFlush();
        }
    }
}