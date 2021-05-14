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
        private readonly ITokenSource _tokenSource;
        private Token _lastToken = null;
        private List<Token> _tokens;
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
            if ((s = ParseIfStatement()) != null)
                return s;
            throw new Exception("No statements");
        }

        private Statement? ParseFuncCallOrVarDefOrAssign()
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

        private Statement? ParseIfStatement()
        {
            if (_lastToken.Type == If)
            {
                GetToken();
                if (!ParseLogicalExpression()) return CreateStatement(BadStatementType);
                if (_lastToken.Type != Colon) return CreateStatement(BadStatementType);
                GetToken();
                if (_lastToken.Type != Newline) return CreateStatement(BadStatementType);
                GetToken();
                if (_lastToken.Type != Indent) return CreateStatement(BadStatementType);
                GetToken();
                var s = GetNextStatement();
                if (s.Type == BadStatementType)
                    return s;
                GetToken();
                if (_lastToken.Type != Newline) return CreateStatement(BadStatementType);
                while (true)
                {
                    GetToken();
                    if (_lastToken.Type != Indent) return CreateStatement(IfStatement);
                    s = GetNextStatement();
                    if (s.Type == BadStatementType)
                        return s;
                    GetToken();
                    if (_lastToken.Type != Newline) return CreateStatement(BadStatementType);
                }
            }
            return null;
        }

        private Statement ParseFunCall()
        {
            var funCall = new FunCall{Name=_tokens[^2].Value.GetString()};
            Log.Debug($"FunctionParser:function_name {funCall.Name}");
            GetToken();
            if (_lastToken.Type == RightParenthesis) return funCall;  // no arguments
            if (!IsParameter(_lastToken)) return CreateStatement(BadStatementType);
            funCall.Args.Add(_lastToken);
            GetToken();
            if (_lastToken.Type == RightParenthesis) return funCall; // single argument
            while (_lastToken.Type != RightParenthesis) // multiple arguments
            {
                if (_lastToken.Type != Comma) return CreateStatement(BadStatementType);
                GetToken();
                if (!IsParameter(_lastToken)) return CreateStatement(BadStatementType);
                funCall.Args.Add(_lastToken);
                GetToken();
            }
            return funCall;
        }


        private Statement ParseVarDefOrAssign()
        {
            GetToken();
            if (((IList) new[] {IntegerConstant, DecimalConstant, StringLiteral, LogicalConstant, Identifier, LeftParenthesis}).Contains(
                _lastToken.Type))
                return ParseAssignment();
            if (((IList) new[] {IntegerType, BooleanType, StringType, DecimalType}).Contains(_lastToken.Type))
                return ParseVariableDef();
            return CreateStatement(BadStatementType);
        }

        private Statement ParseAssignment()
        {
            if (((IList) new[] {IntegerConstant, DecimalConstant, StringLiteral, LogicalConstant}).Contains(_lastToken.Type))
                return new AssignmentStatement {LeftSide = _tokens[0], RightSide = new RValue(_tokens[2])};
            if (_lastToken.Type == Identifier)
            {
                GetToken();
                if (SourceEnd || _lastToken.Type == Newline)
                    return new AssignmentStatement {LeftSide = _tokens[0], RightSide = new RValue(_tokens[2])};
                if (_lastToken.Type == LeftParenthesis)
                {
                    var s = ParseFunCall();
                    if (s.Type == FunctionCall)
                    {
                        var funCall = (FunCall) s;
                        return new AssignmentStatement {LeftSide = _tokens[0], RightSide = new RValue(funCall)};
                    }
                }
                return CreateStatement(BadStatementType);
            }

            if (ParseLogicalExpression() && (SourceEnd || _lastToken.Type == Newline))
            {
                return new AssignmentStatement {LeftSide = _tokens[0], RightSide = new RValue(_tokens[2])};
            }
            return CreateStatement(BadStatementType);
        }
        
        private Statement ParseVariableDef()
        {
            TokenType variableType = _lastToken.Type;
            GetToken();
            if (_lastToken.Type != LeftParenthesis) return CreateStatement(BadStatementType);
            GetToken();
            if (_lastToken.Type != Identifier)
            {
                switch (_lastToken.Type)
                {
                    case IntegerConstant: if (variableType != IntegerType) return CreateStatement(BadStatementType);
                        break;
                    case DecimalConstant: if (variableType != DecimalType) return CreateStatement(BadStatementType);
                        break;
                    case LogicalConstant: if (variableType != BooleanType) return CreateStatement(BadStatementType);
                        break;
                    case StringLiteral: if (variableType != StringType) return CreateStatement(BadStatementType);
                        break;
                    default:
                        return CreateStatement(BadStatementType);
                }
            }
            GetToken();
            if (_lastToken.Type != RightParenthesis) return CreateStatement(BadStatementType);
            return new VariableDef
                { LeftSide = _tokens[0], RightSide = _tokens[4], VariableType = _tokens[2].Type};
        }


        private bool ParseLogicalExpression()
        {
            int brackets = 0;
            Token tokenBeforeLast = new Token();
            while (!SourceEnd && _lastToken.Type != Newline && _lastToken.Type != Colon)
            {
                if (_lastToken.Type == LeftParenthesis)
                {
                    brackets++;
                }
                else if (_lastToken.Type == RightParenthesis)
                {
                    if (tokenBeforeLast.Type == LeftParenthesis) return false;
                    brackets--;
                }
                else if (IsUnaryOoperator(_lastToken))
                {
                    if (tokenBeforeLast.Type != RightParenthesis && !IsParameter(tokenBeforeLast)) return false;
                }
                else if (_lastToken.Type == Not)
                {
                    if (tokenBeforeLast.Type != LeftParenthesis && !IsUnaryOoperator(tokenBeforeLast)) return false;
                }
                else if (IsParameter(_lastToken))
                {
                    if (tokenBeforeLast.Type != LeftParenthesis && tokenBeforeLast.Type != Not && !IsUnaryOoperator(tokenBeforeLast)) return false;
                }
                tokenBeforeLast = _lastToken;
                GetToken(); 
            }
            return brackets == 0;
        }

        private void GetToken(bool addTokens = true)
        {
            _lastToken = _tokenSource.GetNextToken();
            if (_lastToken.Type == End)
                SourceEnd = true;
            else if (addTokens)
                _tokens.Add(_lastToken);
            Log.Debug(
                $"Parser fetched token: {_lastToken} line:{_lastToken.LineNumber} column:{_lastToken.ColumnNumber}");
        }

        private Statement CreateStatement(StatementType type)
        {
            var tokens = _tokens;
            switch (type)
            {
                case BadStatementType:
                    return new BadStatement {BadToken = _lastToken};
            }
            return new Statement { Type = type };
        }
        
        private static bool IsParameter(Token token)
        {
            return ((IList) new[] {IntegerConstant, DecimalConstant, StringLiteral, LogicalConstant, Identifier})
                            .Contains(token.Type);
        }

        private static bool IsUnaryOoperator(Token token)
        {
            return ((IList) new[] {EqualSymbol, NotEqualSymbol, GreaterThan, LessThan, GreaterEqualThan, LessEqualThan, And, Or})
                            .Contains(token.Type);
        }
    }
}