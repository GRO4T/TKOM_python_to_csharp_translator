using System;
using System.Threading;
using Serilog;

namespace PythonCSharpTranslator
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File("logs\\logfile_default.txt")
                .CreateLogger();

            Lexer lexer = new Lexer();
            lexer.Start();

            Console.ReadKey();
            lexer.Stop();
            lexer.Join();
            Log.CloseAndFlush();
        }
    }
}