using System;
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
                .WriteTo.File("logs\\logfile_yyyyDDmm.txt")
                .CreateLogger();

            Log.Information("Hello, world!");

            try
            {
                Log.Warning("Warning message..");
                Log.Debug("debug message..");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error message");
            }

            Log.CloseAndFlush();
            Console.ReadKey();
        }
    }
}