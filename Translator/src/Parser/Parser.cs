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
            if ((s = ParseReturnStatement()) != null)
                return s;
            throw new Exception("No statements");
        }

        private Statement ParseReturnStatement()
        {
            if (_lastToken.Type == Return)
            {
                GetToken();
                if (IsParameter(_lastToken))
                {
                    GetToken();
                    return CreateStatement(ReturnStatement);
                }
                return CreateStatement(BadStatementType);
            }
            return null;
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
            var parseClosure = new Func<List<Tuple<string, TokenType>>, Statement>((argList) =>
            {
                var functionDef = new FunctionDef {Name = _tokens[1].Value.GetString(), ArgList = argList};
                GetToken();
                if (_lastToken.Type == Colon)
                {
                    GetToken(); 
                    return ParseBlockOld() ? CreateStatement(FunctionDefType) : CreateStatement(BadStatementType);
                }
                if (_lastToken.Type == Arrow)
                {
                    GetToken();
                    if (!IsType(_lastToken)) return CreateStatement(BadStatementType);
                    functionDef.ReturnType = _lastToken.Type;
                    GetToken();
                    if (_lastToken.Type != Colon) return CreateStatement(BadStatementType);
                    GetToken();
                    return ParseBlock(ref functionDef.Statements) ? functionDef : CreateStatement(BadStatementType);
                }
                return CreateStatement(BadStatementType);
            });
            if (_lastToken.Type == DefToken)
            {
                var argList = new List<Tuple<string, TokenType>>();
                GetToken();
                if (_lastToken.Type != Identifier) return CreateStatement(BadStatementType);
                GetToken();
                if (_lastToken.Type != LeftParenthesis) return CreateStatement(BadStatementType);
                GetToken();
                if (_lastToken.Type == RightParenthesis) return parseClosure(argList);
                while (true)
                {
                    if (_lastToken.Type != Identifier) return CreateStatement(BadStatementType);
                    var argName = _lastToken.Value.GetString();
                    GetToken();                    
                    if (_lastToken.Type != Colon) return CreateStatement(BadStatementType);
                    GetToken();
                    if (!IsType(_lastToken)) return CreateStatement(BadStatementType);
                    argList.Add(new Tuple<string, TokenType>(argName, _lastToken.Type)); 
                    GetToken();
                    if (_lastToken.Type == RightParenthesis) return parseClosure(argList);
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
                var forLoop = new ForLoop();
                GetToken();
                if (_lastToken.Type != Identifier) return CreateStatement(BadStatementType);
                forLoop.IteratorName = _lastToken.Value.GetString();
                GetToken();
                if (_lastToken.Type != InToken) return CreateStatement(BadStatementType);
                GetToken();
                if (_lastToken.Type != RangeToken) return CreateStatement(BadStatementType);
                GetToken();
                if (_lastToken.Type != LeftParenthesis) return CreateStatement(BadStatementType);
                GetToken();
                if (_lastToken.Type != IntegerConstant) return CreateStatement(BadStatementType);
                forLoop.Start = _lastToken.Value.GetInt();
                GetToken();
                if (_lastToken.Type != Comma) return CreateStatement(BadStatementType);
                GetToken();
                if (_lastToken.Type != IntegerConstant) return CreateStatement(BadStatementType);
                forLoop.End = _lastToken.Value.GetInt();
                GetToken();
                if (_lastToken.Type != RightParenthesis) return CreateStatement(BadStatementType);
                GetToken();
                if (_lastToken.Type != Colon) return CreateStatement(BadStatementType);
                GetToken();
                return ParseBlock(ref forLoop.Statements) ? forLoop : CreateStatement(BadStatementType);
            }
            return null;
        }

        private Statement? ParseWhileLoop()
        {
            if (_lastToken.Type == WhileToken)
            {
                RValue.RValueType type = RValue.RValueType.Undefined;
                GetToken();
                if (!ParseBracketExpression(ref type) || type != RValue.RValueType.LogicalExpression) return CreateStatement(BadStatementType);
                if (_lastToken.Type != Colon) return CreateStatement(BadStatementType);
                GetToken();
                return ParseBlockOld() ? CreateStatement(WhileLoopType) : CreateStatement(BadStatementType);
            }
            return null;
        }

        private Statement? ParseIfStatement()
        {
            if (_lastToken.Type == IfToken)
            {
                var ifStatement = new IfStatement();
                RValue.RValueType type = RValue.RValueType.Undefined;
                GetToken();
                if (!ParseBracketExpression(ref type) || type != RValue.RValueType.LogicalExpression) return CreateStatement(BadStatementType);
                ifStatement.Condition = _tokens.GetRange(1, _tokens.Count - 2); 
                if (_lastToken.Type != Colon) return CreateStatement(BadStatementType);
                GetToken();
                return ParseBlock(ref ifStatement.Statements) ? ifStatement : CreateStatement(BadStatementType);
            }
            return null;
        }

        private Statement ParseFunCall()
        {
            var funCall = new FunctionCall{Name=_tokens[^2].Value.GetString()};
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
            var assignStatement = new AssignmentStatement{LeftSide = _tokens[^3].Value.GetString()};
            if (((IList) new[] {IntegerConstant, DecimalConstant, StringLiteral, LogicalConstant}).Contains(
                _lastToken.Type))
            {
                GetToken();
                if (_lastToken.Type == NewlineToken)
                {
                    assignStatement.RightSide.SetValue(_tokens[2]);
                    return assignStatement;
                }
                if (ParseBracketExpression(ref assignStatement.RightSide.Type) && (SourceEnd || _lastToken.Type == NewlineToken))
                {
                    if (assignStatement.RightSide.Type == RValue.RValueType.LogicalExpression)
                        assignStatement.RightSide.SetLogicalExpression(_tokens.GetRange(2, _tokens.Count - 3));
                    else
                        assignStatement.RightSide.SetArithmeticExpression(_tokens.GetRange(2, _tokens.Count - 3));
                    return assignStatement;
                }
            }
            if (_lastToken.Type == Identifier)
            {
                GetToken();
                if (SourceEnd || _lastToken.Type == NewlineToken)
                {
                    assignStatement.RightSide.SetValue(_tokens[2]);
                    return assignStatement;
                }
                if (_lastToken.Type == LeftParenthesis)
                {
                    var s = ParseFunCall();
                    if (s.Type == FunctionCallType)
                    {
                        var funCall = (FunctionCall) s;
                        assignStatement.RightSide.SetFunCall(funCall);
                        return assignStatement;
                    }
                }
                return CreateStatement(BadStatementType);
            }

            if (ParseBracketExpression(ref assignStatement.RightSide.Type) && (SourceEnd || _lastToken.Type == NewlineToken))
            {
                if (assignStatement.RightSide.Type == RValue.RValueType.LogicalExpression)
                    assignStatement.RightSide.SetLogicalExpression(_tokens.GetRange(2, _tokens.Count - 3));
                else
                    assignStatement.RightSide.SetArithmeticExpression(_tokens.GetRange(2, _tokens.Count - 3));
                return assignStatement;
            }
            return CreateStatement(BadStatementType);
        }
        
        private Statement ParseVariableDef()
        {
            var varDef = new VariableDef {Name = _tokens[0].Value.GetString(), VariableType = _lastToken.Type};
            GetToken();
            if (_lastToken.Type != LeftParenthesis) return CreateStatement(BadStatementType);
            GetToken();
            if (_lastToken.Type != Identifier)
            {
                switch (_lastToken.Type)
                {
                    case IntegerConstant: if (varDef.VariableType != IntToken) return CreateStatement(BadStatementType);
                        break;
                    case DecimalConstant: if (varDef.VariableType != FloatToken) return CreateStatement(BadStatementType);
                        break;
                    case LogicalConstant: if (varDef.VariableType != BoolToken) return CreateStatement(BadStatementType);
                        break;
                    case StringLiteral: if (varDef.VariableType != StrToken) return CreateStatement(BadStatementType);
                        break;
                    default:
                        return CreateStatement(BadStatementType);
                }
            }
            varDef.InitialValue = _lastToken;
            GetToken();
            if (_lastToken.Type != RightParenthesis) return CreateStatement(BadStatementType);
            GetToken();
            return varDef;
        }


        private bool ParseBracketExpression(ref RValue.RValueType type)
        {
            int brackets = 0;
            Token tokenBeforeLast = _tokens[^2]; 
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
                    if (IsArithmeticOperator(_lastToken))
                    {
                        if (type == RValue.RValueType.LogicalExpression) return false;
                        type = RValue.RValueType.ArithmeticExpression;
                    }
                    else
                    {
                        if (type == RValue.RValueType.ArithmeticExpression) return false;
                        type = RValue.RValueType.LogicalExpression;
                    }
                    if (tokenBeforeLast.Type != RightParenthesis && !IsParameter(tokenBeforeLast)) return false;
                }
                else if (_lastToken.Type == NotToken)
                {
                    if (type == RValue.RValueType.ArithmeticExpression) return false;
                    type = RValue.RValueType.LogicalExpression;
                    if (tokenBeforeLast.Type != LeftParenthesis && !IsUnaryOperator(tokenBeforeLast)) return false;
                }
                else if (IsParameter(_lastToken))
                {
                    if (tokenBeforeLast.Type != LeftParenthesis && tokenBeforeLast.Type != NotToken && !IsUnaryOperator(tokenBeforeLast)) return false;
                }
                else
                    return false;
                tokenBeforeLast = _lastToken;
                GetToken(); 
            }
            if (type == RValue.RValueType.Undefined) type = RValue.RValueType.ArithmeticExpression;
            return brackets == 0;
        }
        
        private bool ParseBlock(ref List<Statement> statements)
        {
            if (_lastToken.Type != NewlineToken) return false;
            GetToken();
            if (_lastToken.Type != TabToken) return false; 
            GetToken();
            var s = GetNextStatement();
            if (s.Type == BadStatementType)
                return false;
            statements.Add(s);
            if (_lastToken.Type != NewlineToken) return false;
            while (true)
            {
                GetToken();
                if (_lastToken.Type != TabToken) return true;
                GetToken();
                s = GetNextStatement();
                if (s.Type == BadStatementType)
                    return false;
                statements.Add(s);
                if (_lastToken.Type != NewlineToken) return false;
            }
        }
        private bool ParseBlockOld()
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
                GetToken();
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
            return ((IList) new[]
                {
                    EqualSymbol, NotEqualSymbol, GreaterThan, LessThan,
                    GreaterEqualThan, LessEqualThan, AndToken, OrToken,
                    Plus, Minus, Slash, Star
                })
                            .Contains(token.Type);
        }

        private static bool IsArithmeticOperator(Token token)
        {
            return ((IList) new[]
                {
                    Plus, Minus, Slash, Star
                })
                            .Contains(token.Type);
        }
        
        private static bool IsLogicalOperator(Token token)
        {
            return ((IList) new[]
                {
                    EqualSymbol, NotEqualSymbol, GreaterThan, LessThan,
                    GreaterEqualThan, LessEqualThan, AndToken, OrToken,
                    NotToken
                })
                            .Contains(token.Type);
        }

        private static bool IsType(Token token)
        {
            return ((IList) new[] {IntToken, StrToken, FloatToken, BoolToken})
                            .Contains(token.Type);
        }
    }
}