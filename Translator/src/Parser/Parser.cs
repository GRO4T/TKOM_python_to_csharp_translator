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
                 if (_lastToken.Type == AssignmentSymbol)
                     return ParseVarDefOrAssign();
                 if (_lastToken.Type == LeftParenthesis)
                     return ParseFuncCall();
            }
            return null;
        }

        private Statement ParseVarDefOrAssign()
        {
            GetToken();
            if (((IList) new[] {IntegerConstant, DecimalConstant, StringLiteral, LogicalConstant, Identifier}).Contains(_lastToken.Type))
                return CreateStatement(AssignmentStatement);
            if (((IList) new[] {IntegerType, BooleanType, StringType, DecimalType}).Contains(_lastToken.Type))
                return ParseVariableDef();
            return CreateStatement(UnknownStatement);
        }

        private Statement ParseFuncCall()
        {
            var IsArg = new Func<bool>(() =>
            {
                return ((IList) new[] {IntegerConstant, DecimalConstant, StringLiteral, LogicalConstant, Identifier})
                    .Contains(_lastToken.Type);
            });
            GetToken();
            if (_lastToken.Type == RightParenthesis) return CreateStatement(FunctionCall);
            if (!IsArg()) return CreateStatement(UnknownStatement);
            GetToken();
            if (_lastToken.Type == RightParenthesis) return CreateStatement(FunctionCall);
            while (_lastToken.Type != RightParenthesis)
            {
                if (_lastToken.Type != Comma) return CreateStatement(UnknownStatement);
                GetToken();
                if (!IsArg()) return CreateStatement(UnknownStatement);
                GetToken();
            }
            return CreateStatement(FunctionCall);
        }

        private Statement ParseVariableDef()
        {
            TokenType variableType = _lastToken.Type;
            GetToken();
            if (_lastToken.Type != LeftParenthesis) return CreateStatement(UnknownStatement);
            GetToken();
            if (_lastToken.Type != Identifier)
            {
                switch (_lastToken.Type)
                {
                    case IntegerConstant: if (variableType != IntegerType) return CreateStatement(UnknownStatement);
                        break;
                    case DecimalConstant: if (variableType != DecimalType) return CreateStatement(UnknownStatement);
                        break;
                    case LogicalConstant: if (variableType != BooleanType) return CreateStatement(UnknownStatement);
                        break;
                    case StringLiteral: if (variableType != StringType) return CreateStatement(UnknownStatement);
                        break;
                    default:
                        return CreateStatement(UnknownStatement);
                }
            }
            GetToken();
            if (_lastToken.Type != RightParenthesis) return CreateStatement(UnknownStatement);
            return CreateStatement(VariableDef);
        }

        private bool TryParseTokens(TokenType[] tokens)
        {
            foreach (var token in tokens)
            {
                GetToken();
                if (_lastToken.Type != token) return false;
            }
            return true;
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