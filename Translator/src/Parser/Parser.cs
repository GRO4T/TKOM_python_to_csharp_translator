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
        private Token _currentToken = null;
        private List<Token> _tokens;
        public bool SourceEnd = false;
        
        public Parser(ITokenSource tokenSource)
        {
            _tokenSource = tokenSource;
        }
        public Statement GetNextStatement()
        {
            _tokens = new List<Token>();
            while (_currentToken == null || _currentToken.Type == NewlineToken)
                GetToken(false);
            _tokens.Add(_currentToken);
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
            if (_currentToken.Type == Return)
            {
                GetToken();
                if (IsParameter(_currentToken))
                {
                    GetToken();
                    return new ReturnStatement {Value = _tokens[^2]};
                }
                return CreateBadStatement();
            }
            return null;
        }

        private Statement? ParseFuncCallOrVarDefOrAssign()
        {
            if (_currentToken.Type == Identifier)
            {
                 GetToken();
                 if (_currentToken.Type == AssignmentSymbol)
                     return ParseVarDefOrAssign();
                 if (_currentToken.Type == LeftParenthesis)
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
                if (_currentToken.Type == Colon)
                {
                    GetToken(); 
                    return ParseBlock(ref functionDef.Statements) ? functionDef : CreateBadStatement();
                }
                if (_currentToken.Type == Arrow)
                {
                    GetToken();
                    if (!IsType(_currentToken)) return CreateBadStatement();
                    functionDef.ReturnType = _currentToken.Type;
                    GetToken();
                    if (_currentToken.Type != Colon) return CreateBadStatement();
                    GetToken();
                    return ParseBlock(ref functionDef.Statements) ? functionDef : CreateBadStatement();
                }
                return CreateBadStatement();
            });
            if (_currentToken.Type == DefToken)
            {
                var argList = new List<Tuple<string, TokenType>>();
                GetToken();
                if (_currentToken.Type != Identifier) return CreateBadStatement();
                GetToken();
                if (_currentToken.Type != LeftParenthesis) return CreateBadStatement();
                GetToken();
                if (_currentToken.Type == RightParenthesis) return parseClosure(argList);
                while (true)
                {
                    if (_currentToken.Type != Identifier) return CreateBadStatement();
                    var argName = _currentToken.Value.GetString();
                    GetToken();                    
                    if (_currentToken.Type != Colon) return CreateBadStatement();
                    GetToken();
                    if (!IsType(_currentToken)) return CreateBadStatement();
                    argList.Add(new Tuple<string, TokenType>(argName, _currentToken.Type)); 
                    GetToken();
                    if (_currentToken.Type == RightParenthesis) return parseClosure(argList);
                    if (_currentToken.Type != Comma) return CreateBadStatement();
                    GetToken();
                }
            }
            return null;
        }

        private Statement? ParseForLoop()
        {
            if (_currentToken.Type == ForToken)
            {
                var forLoop = new ForLoop();
                GetToken();
                if (_currentToken.Type != Identifier) return CreateBadStatement();
                forLoop.IteratorName = _currentToken.Value.GetString();
                GetToken();
                if (_currentToken.Type != InToken) return CreateBadStatement();
                GetToken();
                if (_currentToken.Type != RangeToken) return CreateBadStatement();
                GetToken();
                if (_currentToken.Type != LeftParenthesis) return CreateBadStatement();
                GetToken();
                if (_currentToken.Type != IntegerConstant) return CreateBadStatement();
                forLoop.Start = _currentToken.Value.GetInt();
                GetToken();
                if (_currentToken.Type != Comma) return CreateBadStatement();
                GetToken();
                if (_currentToken.Type != IntegerConstant) return CreateBadStatement();
                forLoop.End = _currentToken.Value.GetInt();
                GetToken();
                if (_currentToken.Type != RightParenthesis) return CreateBadStatement();
                GetToken();
                if (_currentToken.Type != Colon) return CreateBadStatement();
                GetToken();
                return ParseBlock(ref forLoop.Statements) ? forLoop : CreateBadStatement();
            }
            return null;
        }

        private Statement? ParseWhileLoop()
        {
            if (_currentToken.Type == WhileToken)
            {
                var whileLoop = new WhileLoop();
                RValue.RValueType type = RValue.RValueType.Undefined;
                GetToken();
                if (!ParseBracketExpression(ref type) || type != RValue.RValueType.LogicalExpression) return CreateBadStatement();
                whileLoop.Condition = _tokens.GetRange(1, _tokens.Count - 2);
                if (_currentToken.Type != Colon) return CreateBadStatement();
                GetToken();
                return ParseBlock(ref whileLoop.Statements) ? whileLoop : CreateBadStatement();
            }
            return null;
        }

        private Statement? ParseIfStatement()
        {
            if (_currentToken.Type == IfToken)
            {
                var ifStatement = new IfStatement();
                RValue.RValueType type = RValue.RValueType.Undefined;
                GetToken();
                if (!ParseBracketExpression(ref type) || type != RValue.RValueType.LogicalExpression) return CreateBadStatement();
                ifStatement.Condition = _tokens.GetRange(1, _tokens.Count - 2); 
                if (_currentToken.Type != Colon) return CreateBadStatement();
                GetToken();
                return ParseBlock(ref ifStatement.Statements) ? ifStatement : CreateBadStatement();
            }
            return null;
        }

        private Statement ParseFunCall()
        {
            var funCall = new FunctionCall{Name=_tokens[^2].Value.GetString()};
            GetToken();
            if (_currentToken.Type == RightParenthesis)
            {
                GetToken();
                return funCall; // no arguments
            }
            if (!IsParameter(_currentToken)) return CreateBadStatement();
            funCall.Args.Add(_currentToken);
            GetToken();
            if (_currentToken.Type == RightParenthesis) // single argument
            {
                GetToken();
                return funCall;
            }
            while (_currentToken.Type != RightParenthesis) // multiple arguments
            {
                if (_currentToken.Type != Comma) return CreateBadStatement();
                GetToken();
                if (!IsParameter(_currentToken)) return CreateBadStatement();
                funCall.Args.Add(_currentToken);
                GetToken();
            }
            GetToken();
            return funCall;
        }


        private Statement ParseVarDefOrAssign()
        {
            GetToken();
            if (((IList) new[] {IntegerConstant, DecimalConstant, StringLiteral, LogicalConstant, Identifier, LeftParenthesis}).Contains(
                _currentToken.Type))
                return ParseAssignment();
            if (((IList) new[] {IntToken, BoolToken, StrToken, FloatToken}).Contains(_currentToken.Type))
                return ParseVariableDef();
            return CreateBadStatement();
        }

        private Statement ParseAssignment()
        {
            var assignStatement = new AssignmentStatement{LeftSide = _tokens[^3].Value.GetString()};
            if (((IList) new[] {IntegerConstant, DecimalConstant, StringLiteral, LogicalConstant}).Contains(
                _currentToken.Type))
            {
                GetToken();
                if (_currentToken.Type == NewlineToken)
                {
                    assignStatement.RightSide.SetValue(_tokens[2]);
                    return assignStatement;
                }
                if (ParseBracketExpression(ref assignStatement.RightSide.Type) && (SourceEnd || _currentToken.Type == NewlineToken))
                {
                    if (assignStatement.RightSide.Type == RValue.RValueType.LogicalExpression)
                        assignStatement.RightSide.SetLogicalExpression(_tokens.GetRange(2, _tokens.Count - 3));
                    else
                        assignStatement.RightSide.SetArithmeticExpression(_tokens.GetRange(2, _tokens.Count - 3));
                    return assignStatement;
                }
            }
            if (_currentToken.Type == Identifier)
            {
                GetToken();
                if (SourceEnd || _currentToken.Type == NewlineToken)
                {
                    assignStatement.RightSide.SetValue(_tokens[2]);
                    return assignStatement;
                }
                if (_currentToken.Type == LeftParenthesis)
                {
                    var s = ParseFunCall();
                    if (s.Type == FunctionCallType)
                    {
                        var funCall = (FunctionCall) s;
                        assignStatement.RightSide.SetFunCall(funCall);
                        return assignStatement;
                    }
                }
                return CreateBadStatement();
            }

            if (ParseBracketExpression(ref assignStatement.RightSide.Type) && (SourceEnd || _currentToken.Type == NewlineToken))
            {
                if (assignStatement.RightSide.Type == RValue.RValueType.LogicalExpression)
                    assignStatement.RightSide.SetLogicalExpression(_tokens.GetRange(2, _tokens.Count - 3));
                else
                    assignStatement.RightSide.SetArithmeticExpression(_tokens.GetRange(2, _tokens.Count - 3));
                return assignStatement;
            }
            return CreateBadStatement();
        }
        
        private Statement ParseVariableDef()
        {
            var varDef = new VariableDef {Name = _tokens[0].Value.GetString(), VariableType = _currentToken.Type};
            GetToken();
            if (_currentToken.Type != LeftParenthesis) return CreateBadStatement();
            GetToken();
            if (_currentToken.Type != Identifier)
            {
                switch (_currentToken.Type)
                {
                    case IntegerConstant: if (varDef.VariableType != IntToken) return CreateBadStatement();
                        break;
                    case DecimalConstant: if (varDef.VariableType != FloatToken) return CreateBadStatement();
                        break;
                    case LogicalConstant: if (varDef.VariableType != BoolToken) return CreateBadStatement();
                        break;
                    case StringLiteral: if (varDef.VariableType != StrToken) return CreateBadStatement();
                        break;
                    default:
                        return CreateBadStatement();
                }
            }
            varDef.InitialValue = _currentToken;
            GetToken();
            if (_currentToken.Type != RightParenthesis) return CreateBadStatement();
            GetToken();
            return varDef;
        }


        private bool ParseBracketExpression(ref RValue.RValueType type)
        {
            int brackets = 0;
            Token lastToken = _tokens[^2]; 
            while (!SourceEnd && _currentToken.Type != NewlineToken && _currentToken.Type != Colon)
            {
                if (_currentToken.Type == LeftParenthesis)
                {
                    brackets++;
                }
                else if (_currentToken.Type == RightParenthesis)
                {
                    if (lastToken.Type == LeftParenthesis) return false;
                    brackets--;
                }
                else if (IsUnaryOperator(_currentToken))
                {
                    if (IsArithmeticOperator(_currentToken))
                    {
                        if (type == RValue.RValueType.LogicalExpression) return false;
                        type = RValue.RValueType.ArithmeticExpression;
                    }
                    else
                    {
                        if (type == RValue.RValueType.ArithmeticExpression) return false;
                        type = RValue.RValueType.LogicalExpression;
                    }
                    if (lastToken.Type != RightParenthesis && !IsParameter(lastToken)) return false;
                }
                else if (_currentToken.Type == NotToken)
                {
                    if (type == RValue.RValueType.ArithmeticExpression) return false;
                    type = RValue.RValueType.LogicalExpression;
                    if (lastToken.Type != LeftParenthesis && !IsUnaryOperator(lastToken)) return false;
                }
                else if (IsParameter(_currentToken))
                {
                    if (lastToken.Type != LeftParenthesis &&
                        lastToken.Type != NotToken &&
                        lastToken.Type != WhileToken &&
                        lastToken.Type != IfToken &&
                        !IsUnaryOperator(lastToken)) return false;
                }
                else
                    return false;
                lastToken = _currentToken;
                GetToken(); 
            }
            if (type == RValue.RValueType.Undefined) type = RValue.RValueType.ArithmeticExpression;
            return brackets == 0;
        }
        
        private bool ParseBlock(ref List<Statement> statements)
        {
            if (_currentToken.Type != NewlineToken) return false;
            GetToken();
            if (_currentToken.Type != TabToken) return false; 
            GetToken();
            var s = GetNextStatement();
            if (s.Type == BadStatementType)
                return false;
            statements.Add(s);
            if (_currentToken.Type != NewlineToken) return false;
            while (true)
            {
                GetToken();
                if (_currentToken.Type != TabToken) return true;
                GetToken();
                s = GetNextStatement();
                if (s.Type == BadStatementType)
                    return false;
                statements.Add(s);
                if (_currentToken.Type != NewlineToken) return false;
            }
        }

        private void GetToken(bool addTokens = true)
        {
            _currentToken = _tokenSource.GetNextToken();
            if (_currentToken.Type == End)
                SourceEnd = true;
            else if (addTokens)
                _tokens.Add(_currentToken);
            Log.Debug(
                $"Parser fetched token: {_currentToken} line:{_currentToken.LineNumber} column:{_currentToken.ColumnNumber}");
        }

        private Statement CreateBadStatement()
        {
            return new BadStatement {BadToken = _currentToken};
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