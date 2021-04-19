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

            Lexer lexer = new Lexer(new StringCharacterSource("0.25"));
            Token.Token token = lexer.GetNextToken();
            while (token.Type != TokenType.End)
            {
                Log.Information($"Parser fetched token: {token.Type}");
                token = lexer.GetNextToken();
            }
            Log.Information($"Parser fetched token: {token.Type}");

            Log.CloseAndFlush();
        }
    }
}