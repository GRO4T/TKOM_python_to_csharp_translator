using System.Diagnostics;
using System.Threading;
using Serilog;

namespace PythonCSharpTranslator
{
    public class Lexer : ThreadWrapper
    {
        protected override void DoWork()
        {
            Log.Information("Lexer running...");
            Thread.Sleep(1000);
        }
    }
}