using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Serilog;
using static PythonCSharpTranslator.TokenType;
using static PythonCSharpTranslator.StatementType;

namespace PythonCSharpTranslator
{
    public class Parser
    {
        protected ITokenSource _tokenSource;
        protected Token _lastToken = null;
        protected List<Token> _tokens;
        public bool SourceEnd = false;
        public Parser(ITokenSource tokenSource)
        {
            _tokenSource = tokenSource;
        }
        public Statement GetNextStatement()
        {
            _tokens = new List<Token>();
            while (_lastToken == null || _lastToken.Type == Newline)
                GetToken(false);
            _tokens.Add(_lastToken);
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
                     return ParseFunCall();
            }
            return null;
        }

        private Statement ParseFunCall()
        {
            var isArg = new Func<bool>(() => ((IList) new[] {IntegerConstant, DecimalConstant, StringLiteral, LogicalConstant, Identifier})
                .Contains(_lastToken.Type));
            var funCall = new FunCall{Name=_tokens[0].Value.GetString()};
            Log.Debug($"FunctionParser:function_name {funCall.Name}");
            GetToken();
            if (_lastToken.Type == RightParenthesis) return funCall;  // no arguments
            if (!isArg()) return CreateStatement(UnknownStatement);
            funCall.Args.Add(_lastToken);
            GetToken();
            if (_lastToken.Type == RightParenthesis) return funCall; // single argument
            while (_lastToken.Type != RightParenthesis)
            {
                if (_lastToken.Type != Comma) return CreateStatement(UnknownStatement);
                GetToken();
                if (!isArg()) return CreateStatement(UnknownStatement);
                funCall.Args.Add(_lastToken);
                GetToken();
            }
            funCall.Tokens = _tokens;
            return funCall;
        }

        private Statement ParseVarDefOrAssign()
        {
            GetToken();
            if (((IList) new[] {IntegerConstant, DecimalConstant, StringLiteral, LogicalConstant, Identifier}).Contains(_lastToken.Type))
                return CreateStatement(AssignmentStatementType);
            if (((IList) new[] {IntegerType, BooleanType, StringType, DecimalType}).Contains(_lastToken.Type))
                return ParseVariableDef();
            return CreateStatement(UnknownStatement);
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
            return CreateStatement(VariableDefType);
        }

        // private bool TryParseTokens(TokenType[] tokens)
        // {
        //     foreach (var token in tokens)
        //     {
        //         GetToken();
        //         if (_lastToken.Type != token) return false;
        //     }
        //     return true;
        // }
        
        protected void GetToken(bool addTokens = true)
        {
            _lastToken = _tokenSource.GetNextToken();
            if (_lastToken.Type == End)
                SourceEnd = true;
            else if (addTokens)
                _tokens.Add(_lastToken);
            Log.Debug(
                $"Parser fetched token: {_lastToken} line:{_lastToken.LineNumber} column:{_lastToken.ColumnNumber}");
        }

        protected Statement CreateStatement(StatementType type)
        {
            var tokens = _tokens;
            // _tokens = new List<Token>();
            switch (type)
            {
                case AssignmentStatementType:
                    return new AssignmentStatement {Tokens = tokens, LeftSide = tokens[0], RightSide = tokens[2]};
                case VariableDefType:
                    return new VariableDef
                        {Tokens = tokens, LeftSide = tokens[0], RightSide = tokens[4], VariableType = tokens[2].Type};
            }
            return new Statement { Type = type, Tokens = tokens };
        }
    }
}