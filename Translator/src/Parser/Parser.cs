using System;
using Serilog;

namespace PythonCSharpTranslator
{
    public class Parser
    {
        private ITokenSource _tokenSource;
        private Token _lastToken;
        public bool SourceEnd = false;
        public Parser(ITokenSource tokenSource)
        {
            _tokenSource = tokenSource;
        }
        public Statement GetNextStatement()
        {
            Statement? s;
            if ((s = ParseFuncCallOrVarDefOrAssign()) != null)
                return s;
            throw new Exception("No statements");
        }

        Statement? ParseFuncCallOrVarDefOrAssign()
        {
            return null;
        }

        private void GetToken()
        {
            _lastToken = _tokenSource.GetNextToken();
            if (_lastToken.Type == TokenType.End)
                SourceEnd = true;
            Log.Debug(
                $"Parser fetched token: {_lastToken} line:{_lastToken.LineNumber} column:{_lastToken.ColumnNumber}");
        }
    }
}