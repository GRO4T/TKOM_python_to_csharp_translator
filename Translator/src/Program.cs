using System;
using System.Collections.Generic;
using System.Diagnostics;
using Serilog;
using static PythonCSharpTranslator.TokenType;

namespace PythonCSharpTranslator 
{
    internal static class Program
    {
        private static string _inputFile = "Resources/input.py";
        private static string _outputFile = "Resources/output.cs";
        
        static void Main(string[] args)
        {
            InitLogger();
            if (!HandleCmdArgs()) return;
            try
            {
                Translate();
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

        private static void Translate()
        {
            var semanticAnalyzer =
                new SemanticAnalyzer(
                    new Parser(
                        new Lexer(
                            new FileCharacterSource(_inputFile)
                        )
                    )
                );
            var program = new ProgramObject();
            Log.Information("Starting parsing...");
            while (!semanticAnalyzer.IsEnd())
            {
                var s = semanticAnalyzer.EvaluateNextStatement();
                if (s != null)
                    program.Statements.Add(s);
            }
            Log.Information("Parsing finished.");
            Translator.Save(Translator.Translate(program), _outputFile);
        }

        private static void InitLogger()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File("logs\\logfile_default.txt")
                .CreateLogger();
        }

        private static bool HandleCmdArgs()
        {
            var cmdArgs = Environment.GetCommandLineArgs();
            if (cmdArgs.Length > 1)
            {
                if (cmdArgs.Length > 3 || cmdArgs[1] == "-h" || cmdArgs[1] == "--help")
                {
                    Console.WriteLine("Usage: Translator.exe <input_file> <output_file>");
                    return false;
                }
                _inputFile = cmdArgs[1];
            }
            if (cmdArgs.Length > 2)
                _outputFile = cmdArgs[2];
            return true;
        } 
    }
}