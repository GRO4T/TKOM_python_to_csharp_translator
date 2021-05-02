using Serilog;

namespace PythonCSharpTranslator 
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
            ITokenSource lexer = new Lexer(new FileCharacterSource("Resources/input.py"));
            // Parser parser(lexer);
            Parser parser = new Parser(lexer);

            Log.CloseAndFlush();
        }
    }
}