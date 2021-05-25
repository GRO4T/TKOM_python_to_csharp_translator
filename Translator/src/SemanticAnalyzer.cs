using Serilog;

namespace PythonCSharpTranslator
{
    public class SemanticAnalyzer
    {
        private Parser _parser; 
        
        public SemanticAnalyzer(Parser parser)
        {
            _parser = parser;
        }

        public bool IsEnd()
        {
            return _parser.IsEnd();
        }
        
        public Statement AnalyzeNextStatement()
        {
            var s = _parser.GetNextStatement();
            if (s.Type == StatementType.BadStatementType)
                Log.Error(s.ToString());
            Log.Information($"Fetched statement:\n {s}");
            return s;
        }
    }
}