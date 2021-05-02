using System;
using System.Collections;
using System.Collections.Generic;
using Serilog;
using static PythonCSharpTranslator.TokenType;
using static PythonCSharpTranslator.StatementType;

namespace PythonCSharpTranslator
{
    public class Parser
    {
        private ITokenSource _tokenSource;
        private Token _lastToken;
        private List<Token> _tokens;
        public bool SourceEnd = false;
        public Parser(ITokenSource tokenSource)
        {
            _tokenSource = tokenSource;
            _tokens = new List<Token>();
            GetToken();
        }
        public Statement GetNextStatement()
        {
            while (_lastToken.Type == Newline)
                GetToken();
            Statement? s;
            if ((s = ParseFuncCallOrVarDefOrAssign()) != null)
                return s;
            throw new Exception("No statements");
        }

        Statement? ParseFuncCallOrVarDefOrAssign()
        {
            if (_lastToken.Type == Identifier)
            {
                 GetToken();
                 if (_lastToken.Type == Assignment)
                     return ParseAssignmentStatement();
            }
            return null;
        }

        private Statement ParseAssignmentStatement()
        {
            GetToken();
            if ( ((IList) new[] { IntegerConstant }).Contains(_lastToken.Type) )
                return CreateStatement(AssignmentStatement);
            return CreateStatement(UnknownStatement);
        }

        private void GetToken()
        {
            _lastToken = _tokenSource.GetNextToken();
            if (_lastToken.Type == End)
                SourceEnd = true;
            else
                _tokens.Add(_lastToken);
            Log.Debug(
                $"Parser fetched token: {_lastToken} line:{_lastToken.LineNumber} column:{_lastToken.ColumnNumber}");
        }

        private Statement CreateStatement(StatementType type)
        {
            var tokens = _tokens;
            _tokens = new List<Token>();
            return new Statement { Type = type, Tokens = tokens };
        }
    }
}