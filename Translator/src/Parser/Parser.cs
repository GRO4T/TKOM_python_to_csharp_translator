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
            while (_lastToken == null || _lastToken.Type == NewlineToken)
                GetToken(false);
            _tokens.Add(_lastToken);
            Statement? s;
            if ((s = ParseFuncCallOrVarDefOrAssign()) != null)
                return s;
            if ((s = ParseIfStatement()) != null)
                return s;
            if ((s = ParseWhileLoop()) != null)
                return s;
            if ((s = ParseForLoop()) != null)
                return s;
            if ((s = ParseFunctionDef()) != null)
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

        private Statement? ParseFunctionDef()
        {
            var parseClosure = new Func<Statement>(() =>
            {
                GetToken();
                if (_lastToken.Type == Colon)
                {
                    GetToken(); 
                    return ParseBlock() ? CreateStatement(FunctionDef) : CreateStatement(BadStatementType);
                }
                if (_lastToken.Type == Arrow)
                {
                    GetToken();
                    if (!IsType(_lastToken)) return CreateStatement(BadStatementType);
                    GetToken();
                    if (_lastToken.Type != Colon) return CreateStatement(BadStatementType);
                    GetToken();
                    return ParseBlock() ? CreateStatement(FunctionDef) : CreateStatement(BadStatementType);
                }
                return CreateStatement(BadStatementType);
            });
            if (_lastToken.Type == DefToken)
            {
                GetToken();
                if (_lastToken.Type != Identifier) return CreateStatement(BadStatementType);
                GetToken();
                if (_lastToken.Type != LeftParenthesis) return CreateStatement(BadStatementType);
                GetToken();
                if (_lastToken.Type == RightParenthesis) return parseClosure();
                while (true)
                {
                    if (_lastToken.Type != Identifier) return CreateStatement(BadStatementType);
                    GetToken();                    
                    if (_lastToken.Type != Colon) return CreateStatement(BadStatementType);
                    GetToken();
                    if (!IsType(_lastToken)) return CreateStatement(BadStatementType);
                    GetToken();
                    if (_lastToken.Type == RightParenthesis) return parseClosure();
                    if (_lastToken.Type != Comma) return CreateStatement(BadStatementType);
                    GetToken();
                }
            }
            return null;
        }

        private Statement? ParseForLoop()
        {
            if (_lastToken.Type == ForToken)
            {
                GetToken();
                if (_lastToken.Type != Identifier) return CreateStatement(BadStatementType);
                GetToken();
                if (_lastToken.Type != InToken) return CreateStatement(BadStatementType);
                GetToken();
                if (_lastToken.Type != RangeToken) return CreateStatement(BadStatementType);
                GetToken();
                if (_lastToken.Type != LeftParenthesis) return CreateStatement(BadStatementType);
                GetToken();
                if (_lastToken.Type != IntegerConstant) return CreateStatement(BadStatementType);
                GetToken();
                if (_lastToken.Type != Comma) return CreateStatement(BadStatementType);
                GetToken();
                if (_lastToken.Type != IntegerConstant) return CreateStatement(BadStatementType);
                GetToken();
                if (_lastToken.Type != RightParenthesis) return CreateStatement(BadStatementType);
                GetToken();
                if (_lastToken.Type != Colon) return CreateStatement(BadStatementType);
                GetToken();
                return ParseBlock() ? CreateStatement(ForLoop) : CreateStatement(BadStatementType);
            }
            return null;
        }

        private Statement? ParseWhileLoop()
        {
            if (_lastToken.Type == WhileToken)
            {
                GetToken();
                if (!ParseLogicalExpression()) return CreateStatement(BadStatementType);
                if (_lastToken.Type != Colon) return CreateStatement(BadStatementType);
                GetToken();
                return ParseBlock() ? CreateStatement(WhileLoop) : CreateStatement(BadStatementType);
            }
            return null;
        }

        private Statement? ParseIfStatement()
        {
            if (_lastToken.Type == IfToken)
            {
                GetToken();
                if (!ParseLogicalExpression()) return CreateStatement(BadStatementType);
                if (_lastToken.Type != Colon) return CreateStatement(BadStatementType);
                GetToken();
                return ParseBlock() ? CreateStatement(IfStatement) : CreateStatement(BadStatementType);
            }
            return null;
        }

        private Statement ParseFunCall()
        {
            var funCall = new FunCall{Name=_tokens[^2].Value.GetString()};
            Log.Debug($"FunctionParser:function_name {funCall.Name}");
            GetToken();
            if (_lastToken.Type == RightParenthesis)
            {
                GetToken();
                return funCall; // no arguments
            }
            if (!IsParameter(_lastToken)) return CreateStatement(BadStatementType);
            funCall.Args.Add(_lastToken);
            GetToken();
            if (_lastToken.Type == RightParenthesis) // single argument
            {
                GetToken();
                return funCall;
            }
            while (_lastToken.Type != RightParenthesis) // multiple arguments
            {
                if (_lastToken.Type != Comma) return CreateStatement(BadStatementType);
                GetToken();
                if (!IsParameter(_lastToken)) return CreateStatement(BadStatementType);
                funCall.Args.Add(_lastToken);
                GetToken();
            }
            GetToken();
            return funCall;
        }


        private Statement ParseVarDefOrAssign()
        {
            GetToken();
            if (((IList) new[] {IntegerConstant, DecimalConstant, StringLiteral, LogicalConstant, Identifier, LeftParenthesis}).Contains(
                _lastToken.Type))
                return ParseAssignment();
            if (((IList) new[] {IntToken, BoolToken, StrToken, FloatToken}).Contains(_lastToken.Type))
                return ParseVariableDef();
            return CreateStatement(BadStatementType);
        }

        private Statement ParseAssignment()
        {
            if (((IList) new[] {IntegerConstant, DecimalConstant, StringLiteral, LogicalConstant}).Contains(
                _lastToken.Type))
            {
                GetToken(); 
                return new AssignmentStatement {LeftSide = _tokens[0], RightSide = new RValue(_tokens[2])};
            }
            if (_lastToken.Type == Identifier)
            {
                GetToken();
                if (SourceEnd || _lastToken.Type == NewlineToken)
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

            if (ParseLogicalExpression() && (SourceEnd || _lastToken.Type == NewlineToken))
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
                    case IntegerConstant: if (variableType != IntToken) return CreateStatement(BadStatementType);
                        break;
                    case DecimalConstant: if (variableType != FloatToken) return CreateStatement(BadStatementType);
                        break;
                    case LogicalConstant: if (variableType != BoolToken) return CreateStatement(BadStatementType);
                        break;
                    case StringLiteral: if (variableType != StrToken) return CreateStatement(BadStatementType);
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
            while (!SourceEnd && _lastToken.Type != NewlineToken && _lastToken.Type != Colon)
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
                else if (IsUnaryOperator(_lastToken))
                {
                    if (tokenBeforeLast.Type != RightParenthesis && !IsParameter(tokenBeforeLast)) return false;
                }
                else if (_lastToken.Type == NotToken)
                {
                    if (tokenBeforeLast.Type != LeftParenthesis && !IsUnaryOperator(tokenBeforeLast)) return false;
                }
                else if (IsParameter(_lastToken))
                {
                    if (tokenBeforeLast.Type != LeftParenthesis && tokenBeforeLast.Type != NotToken && !IsUnaryOperator(tokenBeforeLast)) return false;
                }
                tokenBeforeLast = _lastToken;
                GetToken(); 
            }
            return brackets == 0;
        }
        
        private bool ParseBlock()
        {
            if (_lastToken.Type != NewlineToken) return false;
            GetToken();
            if (_lastToken.Type != TabToken) return false; 
            GetToken();
            var s = GetNextStatement();
            if (s.Type == BadStatementType)
                return false;
            if (_lastToken.Type != NewlineToken) return false;
            while (true)
            {
                GetToken();
                if (_lastToken.Type != TabToken) return true;
                s = GetNextStatement();
                if (s.Type == BadStatementType)
                    return false;
                if (_lastToken.Type != NewlineToken) return false;
            }
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

        private static bool IsUnaryOperator(Token token)
        {
            return ((IList) new[] {EqualSymbol, NotEqualSymbol, GreaterThan, LessThan, GreaterEqualThan, LessEqualThan, AndToken, OrToken})
                            .Contains(token.Type);
        }

        private static bool IsType(Token token)
        {
            return ((IList) new[] {IntToken, StrToken, FloatToken, BoolToken})
                            .Contains(token.Type);
        }
    }
}