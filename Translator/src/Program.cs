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
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File("logs\\logfile_default.txt")
                .CreateLogger();

            var semanticAnalyzer =
                new SemanticAnalyzer(
                    new Parser(
                        new Lexer(
                            new FileCharacterSource("Resources/logical_expression2.py")
                        )
                    )
                );
            var program = new ProgramObject();
            while (!semanticAnalyzer.IsEnd())
            {
                program.Statements.Add(semanticAnalyzer.AnalyzeNextStatement());
            }

            Translator.Save(Translator.Translate(program), "test.cs");

            Log.CloseAndFlush();
        }
    }
}