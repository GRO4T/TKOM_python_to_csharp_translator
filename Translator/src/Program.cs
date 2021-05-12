using System.Collections.Generic;
using System.Diagnostics;
using Serilog;
using static PythonCSharpTranslator.TokenType;

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
            // ITokenSource lexer = new Lexer(new FileCharacterSource("Resources/input.py"));
            // Parser parser(lexer);
            // Parser parser = new Parser(lexer);
            
            // TokenType[] tokens = new[]
            //     {Identifier, LeftParenthesis, Identifier, Comma, DecimalConstant, RightParenthesis };
            // List<Token> l = new(); 
            // foreach (var t in tokens)
            // {
            //     l.Add(new Token(t));
            // }
            //
            //
            // l[0].Value = new TokenValue("hello");
            //
            // Parser parser = new Parser(new TokenSourceMock(l));

            var parser = new Parser(new Lexer(new FileCharacterSource("Resources/logical_expression2.py")));
            while (!parser.SourceEnd)
            {
                Statement s = parser.GetNextStatement();
                Log.Information($"Fetched statement: {s}");
            }

            Log.CloseAndFlush();
        }
    }
}