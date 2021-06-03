using System;
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
                            new FileCharacterSource("Resources/input_dev.py")
                        )
                    )
                );
            var program = new ProgramObject();
            
            try
            {
                Log.Information("Starting parsing...");
                while (!semanticAnalyzer.IsEnd())
                {
                    var s = semanticAnalyzer.EvaluateNextStatement();
                    if (s != null)
                        program.Statements.Add(s);
                }
                Log.Information("Parsing finished.");
                Translator.Save(Translator.Translate(program), "test.cs");
            }
            catch (TranslationError e)
            {
                Log.Error(e.ToString());
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }

            Log.CloseAndFlush();
        }
    }
}