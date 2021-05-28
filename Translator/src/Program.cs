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
                            new FileCharacterSource("Resources/logical_expression2.py")
                        )
                    )
                );
            var program = new ProgramObject();
            
            try
            {
                while (!semanticAnalyzer.IsEnd())
                {
                    var s = semanticAnalyzer.AnalyzeNextStatement();
                    if (s != null)
                        program.Statements.Add(s);
                }
                Translator.Save(Translator.Translate(program), "test.cs");
            }
            catch (TranslationError e)
            {
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }

            Log.CloseAndFlush();
        }
    }
}