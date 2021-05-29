using System;
using System.Collections.Generic;
using Serilog;

namespace PythonCSharpTranslator
{
    public class SemanticAnalyzer
    {
        private Dictionary<string, Statement> _symbolTable;
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
            // check bad statement
            if (s != null && s.Type == StatementType.BadStatementType)
            {
                Log.Error(s.ToString());
                throw new TranslationError();
            }
            // check symbol already declared
            // if (s.Type == Fun)
            Log.Information($"Fetched statement:\n {s}");
            return s;
        }
    }
}